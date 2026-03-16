using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class CropPlanter : RobotOperator
{
    [SerializeField] private PlantingConfig config = new PlantingConfig();
    
    private PlanterOperation operation;

    protected override void Start()
    {
        base.Start();
        CropDatabase cropDB = CropLoader.Load();
        operation = new PlanterOperation(transform, movement, energy, config, cropDB);
        Invoke(nameof(Initialize), 3f);
    }
    
    private void Initialize()
    {
        ScanForEmptyParcels();
    }

    protected override void UpdateOperation() => operation.Update();
    protected override bool IsWorking() => operation.IsPlanting;
    protected override float GetArriveDistance() => config.arriveDistance;

    protected override void OnArrivedAtParcel(EnvironmentalSensor parcel)
    {
        operation.StartPlanting(parcel);
    }

    protected override void OnAllParcelsComplete()
    {
        state = OperatorState.Idle;
        idleTimer = config.rescanInterval;
        movement.ClearTarget();
        Debug.Log($"[CropPlanter] Cycle complete. Waiting {idleTimer:F1}s for next scan.");
    }

    protected override void UpdateIdle() 
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f) ScanForEmptyParcels();
    }

    private void ScanForEmptyParcels()
    {
        FenceZone zone = ZoneHelper.GetZoneAt(transform.position);
        parcels = ParcelHelper.GetParcelsInZone(zone, config.minSoilQuality);
        
        // Filter for parcels that have no active crops (were either never planted or just harvested)
        parcels.RemoveAll(p => p.activeCrops.Count > 0);
        
        parcelIndex = 0;
        if (parcels.Count > 0) MoveToNextParcel();
        else OnAllParcelsComplete();
    }

    protected override string GetWorkingStatus() => $"Planting {operation?.PlantIndex}/{operation?.TotalPositions}";
    protected override string GetIdleStatus() => idleTimer > 0 ? $"Scanning ({idleTimer:F0}s)" : "Scanning...";

    public bool IsPlanting => state == OperatorState.Working;
    public int PlantsPlaced => operation != null ? operation.TotalPlantsPlaced : 0;
    public float TotalSeedCost => operation != null ? operation.TotalCost : 0;
}