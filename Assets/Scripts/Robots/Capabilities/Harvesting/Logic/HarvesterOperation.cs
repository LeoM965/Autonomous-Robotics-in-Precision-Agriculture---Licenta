using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class HarvesterOperation
{
    private Transform transform;
    private RobotMovement movement;
    private RobotEnergy energy;
    private HarvestConfig config;
    private HarvestExecutor executor;
    private CropDatabase cropDB;

    private List<CropGrowth> cropsToHarvest = new List<CropGrowth>();
    private int cropIndex;
    private bool isHarvesting;
    private int sessionHarvestedCount;

    public bool IsHarvesting => isHarvesting;
    public int CropIndex => cropIndex;
    public int TotalCrops => cropsToHarvest.Count;
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
        cropsToHarvest.Clear();
        cropIndex = 0;

        foreach (var crop in parcel.activeCrops)
        {
            if (crop != null && crop.IsHarvestable)
                cropsToHarvest.Add(crop);
        }

        if (cropsToHarvest.Count > 0)
        {
            if (energy != null) energy.SetWorking(true);
            executor.SetTarget(parcel, config.harvestDelay, cropDB);
            isHarvesting = true;
            MoveToNextCrop();
        }
        else FinishParcel();
    }

    public void Update()
    {
        if (!isHarvesting) return;
        if (cropIndex >= cropsToHarvest.Count) { FinishParcel(); return; }

        CropGrowth targetCrop = cropsToHarvest[cropIndex];
        if (targetCrop == null || !targetCrop.gameObject.activeInHierarchy || targetCrop.IsBeingHarvested)
        {
            cropIndex++;
            MoveToNextCrop();
            return;
        }

        Vector3 target = targetCrop.transform.position;
        Vector3 pos = transform.position;
        float dx = pos.x - target.x, dz = pos.z - target.z;

        if (dx * dx + dz * dz < config.harvestRadius * config.harvestRadius || !movement.HasTarget)
        {
            movement.Stop();
            if (executor.UpdateHarvest(targetCrop, transform))
            {
                cropIndex++;
                MoveToNextCrop();
            }
        }
    }

    private void MoveToNextCrop()
    {
        if (cropIndex >= cropsToHarvest.Count) { FinishParcel(); return; }
        movement.SetTarget(cropsToHarvest[cropIndex].transform.position);
    }

    private void FinishParcel()
    {
        if (energy != null) energy.SetWorking(false);
        sessionHarvestedCount += executor.HarvestedInParcel;
        cropsToHarvest.Clear();
        cropIndex = 0;
        executor.Reset();
        isHarvesting = false;
    }
}
