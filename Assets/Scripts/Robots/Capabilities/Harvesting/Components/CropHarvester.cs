using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class CropHarvester : RobotOperator
{
    [SerializeField] private HarvestConfig config = new HarvestConfig();

    private HarvesterOperation operation;

    protected override void Start()
    {
        base.Start();
        var db = CropLoader.Load();
        operation = new HarvesterOperation(transform, movement, energy, config, db);
        Invoke(nameof(ScanForMatureCrops), 5f);
    }

    protected override void UpdateOperation() => operation.Update();
    protected override bool IsWorking() => operation.IsHarvesting;
    protected override float GetArriveDistance() => config.arriveDistance;

    protected override void OnArrivedAtParcel(EnvironmentalSensor parcel)
    {
        operation.StartHarvesting(parcel);
    }

    protected override void OnAllParcelsComplete()
    {
        state = OperatorState.Idle;
        idleTimer = config.rescanInterval;
    }

    protected override void UpdateIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f) ScanForMatureCrops();
    }

    protected override string GetWorkingStatus() => $"Harvesting {operation?.CropIndex}/{operation?.TotalCrops}";
    protected override string GetIdleStatus() => idleTimer > 0 ? $"Scanning ({idleTimer:F0}s)" : "Idle";

    private void ScanForMatureCrops()
    {
        FenceZone zone = ZoneHelper.GetZoneAt(transform.position);
        parcels = ParcelHelper.GetParcelsInZone(zone, config.minSoilQuality);
        parcels.RemoveAll(p => !HasHarvestableCrops(p));
        parcelIndex = 0;

        if (parcels.Count > 0) MoveToNextParcel();
        else OnAllParcelsComplete();
    }

    private static bool HasHarvestableCrops(EnvironmentalSensor p)
    {
        foreach (var c in p.activeCrops)
            if (c != null && c.IsHarvestable) return true;
        return false;
    }

    public int TotalHarvested => operation != null ? operation.TotalHarvested : 0;
    public bool IsHarvesting => state == OperatorState.Working;
}
