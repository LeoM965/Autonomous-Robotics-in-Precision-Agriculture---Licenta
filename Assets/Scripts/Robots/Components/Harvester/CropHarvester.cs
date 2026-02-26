using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Robots.Models;

public class CropHarvester : MonoBehaviour
{
    [SerializeField] private HarvesterSettings settings = new HarvesterSettings();
    [SerializeField] private HarvestConfig config = new HarvestConfig();

    private RobotEnergyManager energyManager;
    private HarvesterOperation operation;
    private RobotMovement movement;
    private RobotEnergy energy;

    private List<EnvironmentalSensor> parcels = new List<EnvironmentalSensor>();
    private EnvironmentalSensor currentParcel;
    private int parcelIndex;
    private HarvesterState state = HarvesterState.Idle;
    private float waitTimer;

    private void Start()
    {
        movement = GetComponent<RobotMovement>();
        energy = GetComponent<RobotEnergy>();
        energyManager = new RobotEnergyManager(transform, energy, movement);
        operation = new HarvesterOperation(transform, movement, energy, config);
        Invoke(nameof(ScanForMatureCrops), 5f);
    }

    private void Update()
    {
        energyManager.Update();
        operation.Update();

        if (energyManager.IsCharging)
        {
            state = HarvesterState.Charging;
            return;
        }

        switch (state)
        {
            case HarvesterState.MovingToParcel:
                CheckArrivalAtParcel();
                break;
            case HarvesterState.Harvesting:
                if (!operation.IsHarvesting) MoveToNextParcel();
                break;
            case HarvesterState.Charging:
                if (!energyManager.IsCharging)
                {
                    state = HarvesterState.Idle;
                    MoveToNextParcel();
                }
                break;
            case HarvesterState.Idle:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f) ScanForMatureCrops();
                break;
        }
    }

    private void ScanForMatureCrops()
    {
        FenceZone zone = ZoneHelper.GetZoneAt(transform.position);
        parcels = ParcelHelper.GetParcelsInZone(zone, settings.minSoilQuality);
        parcels.RemoveAll(p => !HasHarvestableCrops(p));
        parcelIndex = 0;

        if (parcels.Count > 0) MoveToNextParcel();
        else EnterIdle();
    }

    private static bool HasHarvestableCrops(EnvironmentalSensor p)
    {
        foreach (var c in p.activeCrops)
            if (c != null && c.IsFullyGrown && !c.IsBeingHarvested) return true;
        return false;
    }

    private void EnterIdle()
    {
        SetParcelCollision(false);
        state = HarvesterState.Idle;
        waitTimer = settings.rescanInterval;
    }

    private void MoveToNextParcel()
    {
        if (parcelIndex >= parcels.Count) { EnterIdle(); return; }

        SetParcelCollision(false);
        currentParcel = parcels[parcelIndex++];
        if (currentParcel == null) { MoveToNextParcel(); return; }

        float dist = Vector3.Distance(transform.position, currentParcel.transform.position);
        if (!energyManager.CheckBattery(dist, 60f)) { state = HarvesterState.Charging; return; }

        SetParcelCollision(true);
        movement.SetTarget(currentParcel.transform.position);
        state = HarvesterState.MovingToParcel;
    }

    private void CheckArrivalAtParcel()
    {
        if (currentParcel == null) return;
        Vector3 diff = transform.position - currentParcel.transform.position;
        if (diff.x * diff.x + diff.z * diff.z < settings.arriveDistance * settings.arriveDistance || !movement.HasTarget)
        {
            operation.StartHarvesting(currentParcel);
            state = HarvesterState.Harvesting;
        }
    }

    private void SetParcelCollision(bool ignore)
    {
        if (currentParcel == null) return;
        Collider col = currentParcel.GetComponent<Collider>();
        if (col != null) movement.IgnoreCollisionWith(col, ignore);
    }

    public int TotalHarvested => operation != null ? operation.TotalHarvested : 0;
    public bool IsHarvesting => state == HarvesterState.Harvesting;

    public string GetStatus()
    {
        if (energyManager != null && energyManager.IsCharging) return "Charging";
        return state switch
        {
            HarvesterState.MovingToParcel => $"Moving to {(currentParcel ? currentParcel.name : "Parcel")}",
            HarvesterState.Harvesting => $"Harvesting {operation?.CropIndex}/{operation?.TotalCrops}",
            HarvesterState.Idle => waitTimer > 0 ? $"Scanning ({waitTimer:F0}s)" : "Idle",
            _ => "Idle"
        };
    }
}
