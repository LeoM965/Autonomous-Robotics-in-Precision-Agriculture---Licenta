using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class PlanterOperation
{
    private CropPlanter planter;
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

    public PlanterOperation(CropPlanter p, Transform t, RobotMovement m, RobotEnergy e, PlantingConfig c, CropDatabase db)
    {
        planter = p;
        transform = t;
        movement = m;
        energy = e;
        config = c;
        cropDB = db;
        executor = new PlantingExecutor();
    }

    public void StartPlanting(EnvironmentalSensor parcel)
    {
        currentParcel = parcel;
        SetupCropForParcel(parcel);
        Collider col = parcel.GetComponent<Collider>();
        if (col == null)
        {
            FinishParcel();
            return;
        }

        // plantPositions is already filtered by SetupCropForParcel (only unplanted spots)
        // Check if we have enough seeds for the remaining unplanted positions
        if (planter != null && plantPositions.Count > 0 && planter.currentSeeds <= 0)
        {
            if (currentParcel != null)
            {
                currentParcel.isScheduledForTask = false;
            }
            plantPositions.Clear();
            isPlanting = false;
            currentParcel = null;
            executor.Reset();
            planter.RefuelSeeds();
            return;
        }
        
        // Positions are already generated inside SetupCropForParcel
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
            if (planter != null && planter.currentSeeds > 0)
            {
                executor.PlantAt(target);
                planter.currentSeeds--;
                plantIndex++;
                MoveToNextPlantPoint();
            }
            else
            {
                // Fără semințe! Oprim plantarea pe această parcelă și mergem la bază/alimentare
                FinishParcel();
                if (planter != null)
                {
                    planter.RefuelSeeds();
                }
            }
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
        int totalPlantCount = plantPositions.Count; // Numărul total de poziții (pentru calcul nutrienți)

        // Dacă parcela are deja o cultură plantată, refolosim aceeași varietate
        if (!string.IsNullOrEmpty(parcel.plantedVarietyName) && cropDB != null)
        {
            crop = cropDB.Get(parcel.plantedVarietyName);
            if (crop != null)
            {
                idx = cropDB.GetIndex(crop.name);
                forced = true; // Forțăm aceeași cultură
            }
        }

        if (forced)
        {
            crop = cropDB.crops[idx];
        }
        else
        {
            crop = CropSelector.SelectBestCrop(cropDB, parcel, transform, totalPlantCount, currentTaskValue);
            if (crop != null && cropDB != null)
                idx = cropDB.GetIndex(crop.name);
        }

        if (crop == null) return;

        // Filtrăm pozițiile deja ocupate de plante existente
        plantPositions = PlantingPositionGenerator.FilterUnplantedPositions(plantPositions, parcel);

        executor.SetTarget(parcel, crop, CropLoader.LoadPrefab(crop.prefabPath), idx, totalPlantCount);
    }

    private EnvironmentalSensor currentParcel;

    private void FinishParcel()
    {
        if (currentParcel != null)
        {
            currentParcel.isScheduledForTask = false;
        }
        sessionPlantsPlaced += executor.PlantsPlaced;
        sessionTotalCost += executor.TotalCost;
        plantPositions.Clear();
        plantIndex = 0;
        executor.Reset();
        isPlanting = false;
        currentParcel = null;
    }

    public void Abort()
    {
        FinishParcel();
    }
}
