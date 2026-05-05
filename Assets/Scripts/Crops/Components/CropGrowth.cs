using UnityEngine;

[RequireComponent(typeof(CropVisualScaling))]
[RequireComponent(typeof(CropHarvestVisuals))]
public class CropGrowth : MonoBehaviour, ICropHandler
{
    [Header("Configuration")]
    public CropSettings settings;
    
    [Header("State")]
    [SerializeField] private CropGrowthState state = new CropGrowthState();
    
    [Header("Components")]
    [SerializeField] private CropVisualScaling scaler;
    [SerializeField] private CropHarvestVisuals harvestFX;
    private Sensors.Components.EnvironmentalSensor parentSensor;

    private float lastVisualProgress = -1f;
    private float customNitrogenConsumption = -1f;
    private int cropIndex = -1;
    private CropData cachedCropData;

    // Lifetime nutrient health tracking — affects harvest yield
    private float accumulatedHealth;
    private int healthSamples;
    public float NutrientHealthScore => healthSamples > 0 ? accumulatedHealth / healthSamples : 1f;

    // === Public API ===
    public bool IsFullyGrown => state.stage == CropStage.Mature;
    public bool IsBeingHarvested => state.isBeingHarvested;
    public bool IsHarvestable => IsFullyGrown && !IsBeingHarvested;
    public CropStage CurrentStage => state.stage;
    public float Progress => state.progress;
    public float PurchasePrice => state.purchasePrice;
    public CropData CachedCropData => cachedCropData;

    private void Awake()
    {
        state.baseScale = transform.localScale;
        
        if (!scaler) scaler = GetComponent<CropVisualScaling>();
        if (!harvestFX) harvestFX = GetComponent<CropHarvestVisuals>();

        if (scaler) scaler.settings = settings;
        if (harvestFX) harvestFX.settings = settings;
    }

    private void OnEnable()
    {
        // Always refresh — crop may have been recycled to a different parcel by the pool
        parentSensor = GetComponentInParent<Sensors.Components.EnvironmentalSensor>();
        if (CropManager.Instance) CropManager.Instance.RegisterCrop(this);

        if (!state.initialized)
        {
            state.initialized = true;
            ResetGrowth();
        }
    }

    private void OnDisable()
    {
        if (CropManager.Instance) CropManager.Instance.UnregisterCrop(this);
    }

    public void Initialize(float growthTime, float seedCost, float nConsumption = -1f, float nOptimal = -1f, int index = -1)
    {
        if (growthTime <= 0) return;
        state.growthTime = growthTime;
        state.purchasePrice = seedCost;
        customNitrogenConsumption = nConsumption;
        cropIndex = index;

        // Cache CropData pentru acces rapid la temperatura cardinala
        var db = CropLoader.Load();
        if (db != null && index >= 0 && index < db.crops.Length)
            cachedCropData = db.crops[index];

        ResetGrowth();
    }

    public void ResetGrowth()
    {
        state.elapsed = 0f;
        state.progress = 0f;
        lastVisualProgress = -1f;
        state.lastUpdateHours = -1f;
        state.stage = CropStage.Seed;
        state.isBeingHarvested = false;
        accumulatedHealth = 0f;
        healthSamples = 0;
        if (scaler) transform.localScale = state.baseScale * scaler.GetInitialScale();
    }

    public float GetTemperatureMultiplier(float currentTemp, CropDatabase db)
    {
        if (cachedCropData != null)
            return cachedCropData.GetTemperatureMultiplier(currentTemp);

        if (db != null && cropIndex >= 0 && cropIndex < db.crops.Length)
        {
            cachedCropData = db.crops[cropIndex];
            return cachedCropData.GetTemperatureMultiplier(currentTemp);
        }

        return 1f;
    }

    public float GetNitrogenConsumptionRate() => customNitrogenConsumption >= 0 ? customNitrogenConsumption : settings.nitrogenConsumptionRate;
    
    public float GetOptimalNitrogen()
    {
        return (cropIndex >= 0 && Settings.SimulationSettings.N_Opt != null && cropIndex < Settings.SimulationSettings.N_Opt.Length)
            ? Settings.SimulationSettings.N_Opt[cropIndex]
            : (settings != null ? settings.nitrogenOptimalThreshold : 50f);
    }

    public void ApplyJobResults(float addedElapsed, float consumedNitrogen, float consumedPhosphorus = 0f, float consumedPotassium = 0f)
    {
        if (state.isBeingHarvested || state.progress >= 1f || addedElapsed <= 0) return;

        if (parentSensor)
        {
            parentSensor.AdjustNutrients(-consumedNitrogen, -consumedPhosphorus, -consumedPotassium);

            // Sample current nutrient satisfaction for lifetime health score
            float optN = GetOptimalNitrogen();
            float optP = cachedCropData?.requirements?.phosphorus?.optimal ?? (optN * 0.5f);
            float optK = cachedCropData?.requirements?.potassium?.optimal ?? (optN * 0.3f);
            float nS = optN > 0 ? Mathf.Clamp01(parentSensor.nitrogen / optN) : 1f;
            float pS = optP > 0 ? Mathf.Clamp01(parentSensor.phosphorus / optP) : 1f;
            float kS = optK > 0 ? Mathf.Clamp01(parentSensor.potassium / optK) : 1f;
            accumulatedHealth += Mathf.Min(nS, Mathf.Min(pS, kS));
            healthSamples++;
        }

        state.elapsed += addedElapsed;
        state.progress = Mathf.Clamp01(state.elapsed / state.growthTime);

        bool forceUpdate = state.progress >= 1f || lastVisualProgress < 0;
        if (forceUpdate || Mathf.Abs(state.progress - lastVisualProgress) > 0.005f)
        {
            lastVisualProgress = state.progress;
            
            CropStage newStage = scaler.DetermineStage(state.progress);
            Vector3 targetScale = state.baseScale * scaler.CalculateScale(newStage, state.progress);

            state.stage = newStage;
            transform.localScale = targetScale;
        }
    }

    public void ProcessGrowth(float deltaHours, float weatherMultiplier)
    {
        if (state.isBeingHarvested || state.progress >= 1f || deltaHours <= 0) return;
        
        if (!parentSensor) parentSensor = GetComponentInParent<Sensors.Components.EnvironmentalSensor>();
        float nutrientMultiplier = 1f;
        float moistureMultiplier = 1f;
        float phMultiplier = 1f;

        float consumedN = 0f, consumedP = 0f, consumedK = 0f;

        if (parentSensor)
        {
            float nConsumeRate = GetNitrogenConsumptionRate();
            float pConsumeRate = nConsumeRate * 0.5f;  // P consumed at 50% of N rate
            float kConsumeRate = nConsumeRate * 0.3f;  // K consumed at 30% of N rate

            consumedN = nConsumeRate * deltaHours;
            consumedP = pConsumeRate * deltaHours;
            consumedK = kConsumeRate * deltaHours;

            // Nutrient satisfaction = worst of N, P, K satisfaction
            float optimalN = GetOptimalNitrogen();
            float optP = cachedCropData?.requirements?.phosphorus?.optimal ?? (optimalN * 0.5f);
            float optK = cachedCropData?.requirements?.potassium?.optimal ?? (optimalN * 0.3f);

            float nSat = optimalN > 0 ? Mathf.Clamp01(parentSensor.nitrogen / optimalN) : 1f;
            float pSat = optP > 0 ? Mathf.Clamp01(parentSensor.phosphorus / optP) : 1f;
            float kSat = optK > 0 ? Mathf.Clamp01(parentSensor.potassium / optK) : 1f;

            nutrientMultiplier = Mathf.Min(nSat, Mathf.Min(pSat, kSat));

            // Moisture multiplier — based on crop-specific optimal humidity range
            float optMoist = cachedCropData?.requirements?.humidity?.optimal ?? 60f;
            float maxMoist = cachedCropData?.requirements?.humidity?.max ?? 90f;
            moistureMultiplier = CalculateRangeMultiplier(parentSensor.soilMoisture, optMoist * 0.4f, optMoist, maxMoist);

            // pH multiplier — based on crop-specific optimal pH range
            float optPH = cachedCropData?.requirements?.pH?.optimal ?? 6.5f;
            float minPH = cachedCropData?.requirements?.pH?.min ?? 5.0f;
            float maxPH = cachedCropData?.requirements?.pH?.max ?? 8.0f;
            phMultiplier = CalculateRangeMultiplier(parentSensor.soilPH, minPH, optPH, maxPH);
        }

        float tempMultiplier = 1f;
        if (cachedCropData != null && Weather.Components.WeatherSystem.Instance != null)
        {
            float currentTemp = Weather.Components.WeatherSystem.Instance.CurrentTemperature;
            tempMultiplier = cachedCropData.GetTemperatureMultiplier(currentTemp);
        }

        float totalMult = weatherMultiplier * nutrientMultiplier * moistureMultiplier * phMultiplier * tempMultiplier;
        ApplyJobResults(deltaHours * totalMult, consumedN, consumedP, consumedK);
    }

    /// <summary>Cardinal-style piecewise multiplier: 0 at extremes, 1 at optimal.</summary>
    private static float CalculateRangeMultiplier(float value, float min, float optimal, float max)
    {
        if (value <= min || value >= max) return 0.1f; // never fully zero — survival minimum
        if (value <= optimal)
            return Mathf.Lerp(0.1f, 1f, (value - min) / (optimal - min));
        return Mathf.Lerp(1f, 0.1f, (value - optimal) / (max - optimal));
    }

    public void ManualUpdate(float currentTotalHours)
    {
        float deltaHours = state.lastUpdateHours >= 0 ? currentTotalHours - state.lastUpdateHours : 0f;
        state.lastUpdateHours = currentTotalHours;
        if (deltaHours <= 0) return;
        
        float weatherMult = Weather.Components.WeatherSystem.Instance != null 
            ? Weather.Components.WeatherSystem.Instance.GetCropGrowthMultiplier() : 1f;

        ProcessGrowth(deltaHours, weatherMult);
    }

    public Sensors.Components.EnvironmentalSensor ParentSensor => parentSensor;

    public void Harvest()
    {
        if (state.isBeingHarvested) return;
        state.isBeingHarvested = true;
        StartCoroutine(harvestFX.PlayHarvestRoutine(transform, OnHarvestComplete));
    }

    private void OnHarvestComplete()
    {
        state.isBeingHarvested = false;
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (parentSensor != null) parentSensor.RemoveCrop(this);
        state.isBeingHarvested = false;
        state.initialized = false;
        if (CropPool.Instance != null) CropPool.Instance.Return(gameObject);
        else Destroy(gameObject);
    }
}
