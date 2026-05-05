using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using AI.Analytics;
using AI.Models.Decisions;

public class HarvesterOperation
{
    private Transform transform;
    private RobotMovement movement;
    private RobotEnergy energy;
    private HarvestConfig config;
    private HarvestExecutor executor;
    private CropDatabase cropDB;
    private EnvironmentalSensor currentParcel;

    // Same grid approach as PlanterOperation
    private List<Vector3> harvestPositions = new List<Vector3>();
    private int posIndex;
    private bool isHarvesting;
    private int sessionHarvestedCount;
    private bool decisionLogged;

    public bool IsHarvesting => isHarvesting;
    public int CropIndex => posIndex;
    public int TotalCrops => harvestPositions.Count;
    public int TotalHarvested => sessionHarvestedCount + executor.HarvestedInParcel;

    public HarvesterOperation(Transform t, RobotMovement m, RobotEnergy e, HarvestConfig c, CropDatabase db)
    {
        transform = t;
        movement = m;
        energy = e;
        config = c;
        cropDB = db;
        executor = new HarvestExecutor();
    }

    public void StartHarvesting(EnvironmentalSensor parcel)
    {
        currentParcel = parcel;
        harvestPositions.Clear();
        posIndex = 0;

        // Ignore crop colliders so robot drives through them
        foreach (var crop in parcel.activeCrops)
        {
            if (crop != null)
            {
                Collider[] cols = crop.GetComponentsInChildren<Collider>();
                foreach (var c in cols)
                    if (c != null) movement.IgnoreCollisionWith(c, true);
            }
        }

        // Generate the SAME grid the planter uses — guaranteed inside parcel bounds
        Collider parcelCol = parcel.GetComponent<Collider>();
        if (parcelCol == null) { FinishParcel(); return; }

        harvestPositions = GenerateHarvestGrid(parcelCol.bounds);

        if (harvestPositions.Count == 0) { FinishParcel(); return; }

        executor.SetTarget(parcel, cropDB);
        isHarvesting = true;

        if (!decisionLogged)
        {
            decisionLogged = true;
            LogHarvestDecision(parcel);
        }

        movement.SetTarget(harvestPositions[0]);
    }

    /// <summary>
    /// Exact same grid generation as PlantingPositionGenerator.Generate — 
    /// same rowCount, same zig-zag order, same margins.
    /// Robot follows the planter's path in reverse, harvesting along the way.
    /// </summary>
    private List<Vector3> GenerateHarvestGrid(Bounds bounds)
    {
        var positions = new List<Vector3>();
        float marginX = bounds.size.x * 0.1f;
        float marginZ = bounds.size.z * 0.1f;
        float minX = bounds.min.x + marginX;
        float maxX = bounds.max.x - marginX;
        float minZ = bounds.min.z + marginZ;
        float maxZ = bounds.max.z - marginZ;

        int rowCount = 5;
        int plantsPerRow = Settings.SimulationSettings.PlantsPerRow;

        for (int row = 0; row < rowCount; row++)
        {
            float x = rowCount <= 1 ? (minX + maxX) * 0.5f 
                : Mathf.Lerp(minX, maxX, (float)row / (rowCount - 1));
            bool forward = (row % 2 == 0);

            for (int plant = 0; plant < plantsPerRow; plant++)
            {
                int idx = forward ? plant : plantsPerRow - 1 - plant;
                float z = plantsPerRow <= 1 ? (minZ + maxZ) * 0.5f 
                    : Mathf.Lerp(minZ, maxZ, (float)idx / (plantsPerRow - 1));
                float y = TerrainHelper.GetSurfaceHeight(new Vector3(x, 0, z));
                positions.Add(new Vector3(x, y, z));
            }
        }

        return positions;
    }

    // Identical to PlanterOperation.Update — navigate position by position
    public void Update()
    {
        if (!isHarvesting) return;
        if (posIndex >= harvestPositions.Count) { FinishParcel(); return; }

        // Always harvest anything nearby while moving (continuous sweep)
        HarvestNearbyCrops();

        Vector3 target = harvestPositions[posIndex];
        Vector3 pos = transform.position;
        float dx = pos.x - target.x, dz = pos.z - target.z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);

        // Generous arrival threshold (planter uses 3f) + pathfinder fallback
        if (dist < 3.5f || movement.HasArrived)
        {
            posIndex++;
            MoveToNextPosition();
        }
        else if (!movement.HasTarget)
        {
            movement.SetTarget(target);
        }
    }

    private void MoveToNextPosition()
    {
        if (posIndex >= harvestPositions.Count) { FinishParcel(); return; }
        movement.SetTarget(harvestPositions[posIndex]);
    }

    private void HarvestNearbyCrops()
    {
        if (currentParcel == null) return;
        // Use generous sweep radius (not harvestRadius) to catch all crops
        // between grid waypoints — grid spacing can be ~4 units
        const float sweepRadius = 5f;
        float radiusSqr = sweepRadius * sweepRadius;
        Vector3 pos = transform.position;

        var snapshot = new List<CropGrowth>(currentParcel.activeCrops);
        foreach (var crop in snapshot)
        {
            if (crop == null || !crop.gameObject.activeInHierarchy) continue;
            if (!crop.IsHarvestable || crop.IsBeingHarvested) continue;

            float dx = pos.x - crop.transform.position.x;
            float dz = pos.z - crop.transform.position.z;
            if (dx * dx + dz * dz < radiusSqr)
                executor.UpdateHarvest(crop, transform);
        }
    }

    private void LogHarvestDecision(EnvironmentalSensor parcel)
    {
        if (DecisionTracker.Instance == null) return;

        int harvestable = 0;
        foreach (var c in parcel.activeCrops)
            if (c != null && c.IsHarvestable) harvestable++;

        float dist = Vector3.Distance(transform.position, parcel.transform.position);
        string variety = parcel.plantedVarietyName ?? "Unknown";
        var data = cropDB?.Get(variety);
        float estWeight = data != null ? data.yieldWeightKg * harvestable : harvestable;
        float estPrice = data != null ? data.marketPricePerKg : 1f;
        float estRevenue = estWeight * estPrice;
        float soilScore = parcel.LatestAnalysis.qualityScore;
        float priority = (harvestable * soilScore) / Mathf.Max(dist, 1f);

        var record = new DecisionRecord
        {
            decisionType = "Harvest",
            chosenOption = "Harvest " + variety,
            parcelName = parcel.name,
            chosenScore = soilScore,
            schedulingValue = priority,
            netValue = estRevenue,
            factors = new DecisionFactors
            {
                nitrogenScore = Mathf.Clamp(parcel.nitrogen, 0f, 100f),
                phosphorusScore = Mathf.Clamp(parcel.phosphorus, 0f, 100f),
                potassiumScore = Mathf.Clamp(parcel.potassium, 0f, 100f),
                humidityScore = Mathf.Clamp(parcel.soilMoisture, 0f, 100f),
                phScore = Mathf.Clamp(parcel.soilPH / 7f * 100f, 0f, 100f)
            }
        };

        DecisionTracker.Instance.RecordDecision(transform, record);
    }

    private void FinishParcel()
    {
        if (currentParcel != null)
        {
            foreach (var crop in currentParcel.activeCrops)
            {
                if (crop != null)
                {
                    Collider[] cols = crop.GetComponentsInChildren<Collider>();
                    foreach (var c in cols)
                        if (c != null) movement.IgnoreCollisionWith(c, false);
                }
            }
        }

        sessionHarvestedCount += executor.HarvestedInParcel;
        harvestPositions.Clear();
        posIndex = 0;
        decisionLogged = false;
        executor.Reset();
        isHarvesting = false;
        currentParcel = null;
    }

    public void Abort()
    {
        FinishParcel();
    }
}
