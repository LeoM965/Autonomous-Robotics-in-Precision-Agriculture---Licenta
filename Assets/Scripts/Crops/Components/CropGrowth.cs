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

    private float customNitrogenConsumption = -1f;
    private int cropIndex = -1;

    public bool IsFullyGrown => state.stage == CropStage.Mature;
    public bool IsBeingHarvested => state.isBeingHarvested;
    public bool IsHarvestable => IsFullyGrown && !IsBeingHarvested;
    public CropStage CurrentStage => state.stage;
    public float Progress => state.progress;
    public float PurchasePrice => state.purchasePrice;

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
        ResetGrowth();
    }

    public void ResetGrowth()
    {
        state.elapsed = 0f;
        state.progress = 0f;
        state.lastUpdateHours = -1f;
        state.stage = CropStage.Seed;
        transform.localScale = state.baseScale * scaler.GetInitialScale();
    }

    public void ManualUpdate(float currentTotalHours)
    {
        if (state.isBeingHarvested || state.progress >= 1f) return;

        // Calculate actual delta hours since this specific crop was last updated
        float deltaHours = 0f;
        if (state.lastUpdateHours >= 0)
        {
            deltaHours = currentTotalHours - state.lastUpdateHours;
        }
        state.lastUpdateHours = currentTotalHours;

        if (deltaHours <= 0) return;

        if (!parentSensor) parentSensor = GetComponentInParent<Sensors.Components.EnvironmentalSensor>();
        float nMultiplier = 1f;

        if (parentSensor)
        {
            // Consume Nitrogen
            float consumeRate = customNitrogenConsumption >= 0 ? customNitrogenConsumption : settings.nitrogenConsumptionRate;
            parentSensor.AdjustNutrients(-consumeRate * deltaHours, 0, 0);

            // Calculate Growth Multiplier based on current Nitrogen
            float optimalN = (cropIndex >= 0 && Settings.SimulationSettings.N_Opt != null && cropIndex < Settings.SimulationSettings.N_Opt.Length)
                ? Settings.SimulationSettings.N_Opt[cropIndex]
                : (settings != null ? settings.nitrogenOptimalThreshold : 50f);

            if (optimalN > 0)
            {
                nMultiplier = Mathf.Clamp01(parentSensor.nitrogen / optimalN);
            }
        }

        float weatherMultiplier = Weather.Components.WeatherSystem.Instance != null
            ? Weather.Components.WeatherSystem.Instance.GetCropGrowthMultiplier()
            : 1f;

        state.elapsed += deltaHours * weatherMultiplier * nMultiplier;
        state.progress = Mathf.Clamp01(state.elapsed / state.growthTime);

        CropStage newStage = scaler.DetermineStage(state.progress);
        Vector3 targetScale = state.baseScale * scaler.CalculateScale(newStage, state.progress);

        if (state.stage != newStage || (transform.localScale - targetScale).sqrMagnitude > 0.00001f)
        {
            state.stage = newStage;
            transform.localScale = targetScale;
        }
    }

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
        GetComponentInParent<Sensors.Components.EnvironmentalSensor>()?.RemoveCrop(this);
        if (CropPool.Instance != null) CropPool.Instance.Return(gameObject);
        else Destroy(gameObject);
    }
}
