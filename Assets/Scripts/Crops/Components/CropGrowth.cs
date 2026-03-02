using UnityEngine;

[RequireComponent(typeof(CropVisualScaling))]
[RequireComponent(typeof(CropHarvestVisuals))]
public class CropGrowth : MonoBehaviour, IGrowable
{
    [Header("Configuration")]
    public CropSettings settings;
    
    [Header("State")]
    [SerializeField] private CropGrowthState state = new CropGrowthState();
    
    [Header("Components")]
    [SerializeField] private CropVisualScaling scaler;
    [SerializeField] private CropHarvestVisuals harvestFX;

    public bool IsFullyGrown => state.stage == CropStage.Mature;
    public bool IsBeingHarvested => state.isBeingHarvested;
    public CropStage CurrentStage => state.stage;
    public float Progress => state.progress;

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
            return;
        }
        if (state.growthTime > 0) ResetGrowth();
    }

    private void OnDisable()
    {
        if (CropManager.Instance) CropManager.Instance.UnregisterCrop(this);
    }

    public void Init(float time)
    {
        if (time <= 0) return;
        state.growthTime = time;
        ResetGrowth();
    }

    public void ResetGrowth()
    {
        state.elapsed = 0f;
        state.progress = 0f;
        state.stage = CropStage.Seed;
        transform.localScale = state.baseScale * scaler.GetInitialScale();
    }

    public void ManualUpdate(float deltaTime)
    {
        if (state.isBeingHarvested || state.progress >= 1f) return;

        float weatherMultiplier = Weather.Components.WeatherSystem.Instance != null
            ? Weather.Components.WeatherSystem.Instance.GetCropGrowthMultiplier()
            : 1f;

        state.elapsed += deltaTime * weatherMultiplier;
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
