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
        if (!parentSensor) parentSensor = GetComponentInParent<Sensors.Components.EnvironmentalSensor>();
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

    public void ApplyJobResults(float addedElapsed, float consumedNitrogen)
    {
        if (state.isBeingHarvested || state.progress >= 1f || addedElapsed <= 0) return;

        if (parentSensor)
        {
            parentSensor.AdjustNutrients(-consumedNitrogen, 0, 0);
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
        float nMultiplier = 1f;

        if (parentSensor)
        {
            float consumeRate = GetNitrogenConsumptionRate();
            parentSensor.AdjustNutrients(-consumeRate * deltaHours, 0, 0);

            float optimalN = GetOptimalNitrogen();
            if (optimalN > 0)
                nMultiplier = Mathf.Clamp01(parentSensor.nitrogen / optimalN);
        }

        float tempMultiplier = 1f;
        if (cachedCropData != null && Weather.Components.WeatherSystem.Instance != null)
        {
            float currentTemp = Weather.Components.WeatherSystem.Instance.CurrentTemperature;
            tempMultiplier = cachedCropData.GetTemperatureMultiplier(currentTemp);
        }

        ApplyJobResults(deltaHours * weatherMultiplier * nMultiplier * tempMultiplier, 0);
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
