using UnityEngine;
using Sensors.Components;

public class PlantingExecutor
{
    EnvironmentalSensor parcel;
    CropData crop;
    GameObject prefab;
    int cropIndex;
    int plantsPlaced;
    float totalCost;
    int totalPlantsInParcel;

    public int PlantsPlaced => plantsPlaced;
    public float TotalCost => totalCost;

    public void SetTarget(EnvironmentalSensor p, CropData c, GameObject pf, int idx, int totalPlants = 1)
    {
        parcel = p; crop = c; prefab = pf; cropIndex = idx; totalPlantsInParcel = totalPlants > 0 ? totalPlants : 1;
    }

    public void PlantAt(Vector3 position)
    {
        if (prefab == null || parcel == null) return;

        var rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        var plant = CropPool.Instance != null
            ? CropPool.Instance.Get(prefab, position, rotation, parcel.transform)
            : Object.Instantiate(prefab, position, rotation, parcel.transform);

        if (crop == null) return;

        if (plantsPlaced == 0) parcel.plantedVarietyName = crop.name;

        var costs = Settings.SimulationSettings.SeedCosts;
        float currentCost = (costs != null && cropIndex >= 0 && cropIndex < costs.Length) ? costs[cropIndex] : crop.seedCostEUR;
        totalCost += currentCost;

        var growth = plant.GetComponent<CropGrowth>();
        if (growth == null)
        {
            // Inject components if missing (e.g. for Onion/Radish raw models)
            growth = plant.AddComponent<CropGrowth>();
            var settings = Resources.Load<CropSettings>("CropSettings");
            
            growth.settings = settings;
            var scaler = plant.GetComponent<CropVisualScaling>();
            if (scaler) scaler.settings = settings;
            var harvest = plant.GetComponent<CropHarvestVisuals>();
            if (harvest) harvest.settings = settings;
        }

        if (growth != null)
        {
            float nConsumption = crop.nitrogenConsumptionRate / totalPlantsInParcel;
            float nOptimal = crop.requirements?.nitrogen?.optimal ?? -1f;
            growth.Initialize(crop.GrowthHours, currentCost, nConsumption, nOptimal, cropIndex);
            
            // Start the growth cycle by registering with the manager
            if (CropManager.Instance != null) CropManager.Instance.RegisterCrop(growth);
            
            parcel.activeCrops.Add(growth);
            plantsPlaced++;
        }
    }

    public void Reset()
    {
        parcel = null; crop = null; prefab = null;
        cropIndex = -1; plantsPlaced = 0; totalCost = 0; totalPlantsInParcel = 1;
    }
}
