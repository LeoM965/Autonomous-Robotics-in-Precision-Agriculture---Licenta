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
        
        plantPositions = PlantingPositionGenerator.Generate(col.bounds, config);
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
        float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(target.x, target.z));
        
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
        CropData selectedCrop = CropSelector.SelectBestCrop(cropDB, parcel.composition, transform, parcel.name);
        if (selectedCrop == null) return;
        
        GameObject prefab = CropLoader.LoadPrefab(selectedCrop.prefabPath);
        executor.SetTarget(parcel, selectedCrop, prefab);
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
}
