using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class PlanterOperation
{
    private Transform transform;
    private RobotMovement movement;
    private RobotEnergy energy;
    private PlantingConfig config;
    private PlantingExecutor executor;
    private CropDatabase cropDB;

    private List<Vector3> plantPositions = new List<Vector3>();
    private int plantIndex;
    private bool isPlanting;

    private int sessionPlantsPlaced;
    private float sessionTotalCost;

    public bool IsPlanting => isPlanting;
    public int PlantIndex => plantIndex;
    public int TotalPositions => plantPositions.Count;
    public int TotalPlantsPlaced => sessionPlantsPlaced + executor.PlantsPlaced;
    public float TotalCost => sessionTotalCost + executor.TotalCost;
    private float currentTaskValue;

    public void SetTaskValue(float value) => currentTaskValue = value;

    public PlanterOperation(Transform t, RobotMovement m, RobotEnergy e, PlantingConfig c, CropDatabase db)
    {
        transform = t;
        movement = m;
        energy = e;
        config = c;
        cropDB = db;
        executor = new PlantingExecutor();
    }

    public void StartPlanting(EnvironmentalSensor parcel)
    {
        SetupCropForParcel(parcel);
        Collider col = parcel.GetComponent<Collider>();
        if (col == null)
        {
            FinishParcel();
            return;
        }
        
        // Positions are already generated inside SetupCropForParcel
        if (energy != null) energy.SetWorking(true);
        isPlanting = true;
        plantIndex = 0;
        MoveToNextPlantPoint();
    }

    public void Update()
    {
        if (!isPlanting) return;
        if (plantIndex >= plantPositions.Count)
        {
            FinishParcel();
            return;
        }
        
        Vector3 target = plantPositions[plantIndex];
        Vector3 pos = transform.position;
        float dx = pos.x - target.x, dz = pos.z - target.z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);
        
        if (dist < config.plantDistance)
        {
            executor.PlantAt(target);
            plantIndex++;
            MoveToNextPlantPoint();
        }
    }

    private void MoveToNextPlantPoint()
    {
        if (plantIndex >= plantPositions.Count)
        {
            FinishParcel();
            return;
        }
        movement.SetTarget(plantPositions[plantIndex]);
    }

    private void SetupCropForParcel(EnvironmentalSensor parcel)
    {
        int idx = Settings.SimulationSettings.SelectedCropIndex;
        bool forced = idx >= 0 && cropDB?.crops != null && idx < cropDB.crops.Length;
        CropData crop;

        // Pre-calculate plant positions to know the count for the economic decision
        Collider col = parcel.GetComponent<Collider>();
        if (col != null)
        {
            plantPositions = PlantingPositionGenerator.Generate(col.bounds, config);
        }
        int plantCount = plantPositions.Count;

        if (forced)
        {
            crop = cropDB.crops[idx];
        }
        else
        {
            crop = CropSelector.SelectBestCrop(cropDB, parcel.composition, transform, parcel.name, plantCount, currentTaskValue);
            if (crop != null && cropDB != null)
                idx = cropDB.GetIndex(crop.name);
        }

        if (crop == null) return;

        executor.SetTarget(parcel, crop, CropLoader.LoadPrefab(crop.prefabPath), idx, plantCount);
    }

    private void FinishParcel()
    {
        if (energy != null) energy.SetWorking(false);
        sessionPlantsPlaced += executor.PlantsPlaced;
        sessionTotalCost += executor.TotalCost;
        plantPositions.Clear();
        plantIndex = 0;
        executor.Reset();
        isPlanting = false;
    }

    public void Abort()
    {
        FinishParcel();
    }
}
