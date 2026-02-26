using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Robots.Models;

public class CropPlanter : MonoBehaviour
{
    [SerializeField] private PlanterSettings settings = new PlanterSettings();
    [SerializeField] private PlantingConfig config = new PlantingConfig();
    
    private RobotEnergyManager energyManager;
    private PlanterOperation operation;
    private RobotMovement movement;
    private RobotEnergy energy;
    
    private List<EnvironmentalSensor> parcels = new List<EnvironmentalSensor>();
    private EnvironmentalSensor currentParcel;
    private int parcelIndex;
    private PlanterState state = PlanterState.Idle;
    
    private void Start()
    {
        movement = GetComponent<RobotMovement>();
        energy = GetComponent<RobotEnergy>();
        CropDatabase cropDB = CropLoader.Load();
        
        energyManager = new RobotEnergyManager(transform, energy, movement);
        operation = new PlanterOperation(transform, movement, energy, config, cropDB);
        
        Invoke(nameof(Initialize), 3f);
    }
    
    private void Initialize()
    {
        FenceZone zone = ZoneHelper.GetZoneAt(transform.position);
        parcels = ParcelHelper.GetParcelsInZone(zone, settings.minSoilQuality);
        
        if (parcels.Count > 0) MoveToNextParcel();
    }
    
    private void Update()
    {
        energyManager.Update();
        operation.Update();
        
        if (energyManager.IsCharging)
        {
            state = PlanterState.Charging; 
            return;
        }

        switch (state)
        {
            case PlanterState.MovingToParcel:
                CheckArrivalAtParcel();
                break;
            case PlanterState.Planting:
                if (!operation.IsPlanting) MoveToNextParcel(); 
                break;
            case PlanterState.Charging:
                 if (!energyManager.IsCharging)
                 {
                     state = PlanterState.Idle;
                     MoveToNextParcel();
                 }
                 break;
        }
    }
    
    private void MoveToNextParcel()
    {
        if (parcelIndex >= parcels.Count) { FinishPlanting(); return; }
        
        currentParcel = parcels[parcelIndex];
        if (currentParcel == null) { parcelIndex++; MoveToNextParcel(); return; }
        
        float dist = Vector3.Distance(transform.position, currentParcel.transform.position);
        if (!energyManager.CheckBattery(dist, 60f)) { state = PlanterState.Charging; return; }
        
        movement.SetTarget(currentParcel.transform.position);
        state = PlanterState.MovingToParcel;
    }
    
    private void CheckArrivalAtParcel()
    {
        if (currentParcel == null) return;
        
        Vector3 diff = transform.position - currentParcel.transform.position;
        if (diff.x * diff.x + diff.z * diff.z < settings.arriveDistance * settings.arriveDistance || !movement.HasTarget)
        {
            operation.StartPlanting(currentParcel);
            state = PlanterState.Planting;
            parcelIndex++;
        }
    }
    
    private void FinishPlanting()
    {
        state = PlanterState.Idle;
        movement.ClearTarget();
        Debug.Log($"[CropPlanter] Complete! {operation.TotalPlantsPlaced} plants, Cost: {operation.TotalCost:F2} EUR");
    }
    
    public bool IsPlanting => state == PlanterState.Planting;
    public int PlantsPlaced => operation != null ? operation.TotalPlantsPlaced : 0;
    public float TotalSeedCost => operation != null ? operation.TotalCost : 0;

    public string GetStatus()
    {
        if (energyManager != null && energyManager.IsCharging) return "Charging";
        return state switch
        {
            PlanterState.MovingToParcel => $"Moving to {(currentParcel ? currentParcel.name : "Parcel")}",
            PlanterState.Planting => $"Planting {operation?.PlantIndex}/{operation?.TotalPositions}",
            PlanterState.Idle => "Idle / Done",
            _ => "Unknown"
        };
    }
}