using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public abstract class RobotOperator : MonoBehaviour
{
    protected RobotEnergyManager energyManager;
    protected RobotMovement movement;
    protected RobotEnergy energy;
    
    protected List<EnvironmentalSensor> parcels = new List<EnvironmentalSensor>();
    protected EnvironmentalSensor currentParcel;
    protected int parcelIndex;
    
    public enum OperatorState { Idle, MovingToParcel, Working, Charging }
    protected OperatorState state = OperatorState.Idle;
    public OperatorState CurrentState => state;
    protected float idleTimer;

    protected virtual void Awake()
    {
        movement = GetComponent<RobotMovement>();
        energy = GetComponent<RobotEnergy>();
        energyManager = new RobotEnergyManager(transform, energy, movement);
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        if (energyManager == null) return;
        energyManager.Update();
        UpdateOperation();

        if (energyManager.IsCharging)
        {
            state = OperatorState.Charging;
            return;
        }

        switch (state)
        {
            case OperatorState.MovingToParcel:
                CheckArrivalAtParcel();
                break;
            case OperatorState.Working:
                if (!IsWorking()) MoveToNextParcel();
                break;
            case OperatorState.Charging:
                if (!energyManager.IsCharging)
                {
                    state = OperatorState.Idle;
                    MoveToNextParcel();
                }
                break;
            case OperatorState.Idle:
                UpdateIdle();
                break;
        }
    }

    protected void MoveToNextParcel()
    {
        if (parcelIndex >= parcels.Count) { OnAllParcelsComplete(); return; }

        EnvironmentalSensor nextParcel = parcels[parcelIndex];
        if (nextParcel == null) 
        { 
            parcelIndex++; 
            MoveToNextParcel(); 
            return; 
        }

        float dist = Vector3.Distance(transform.position, nextParcel.transform.position);
        if (energyManager != null && !energyManager.CheckBattery(dist, 60f)) 
        { 
            state = OperatorState.Charging; 
            return; 
        }

        SetParcelCollision(currentParcel, false);
        currentParcel = nextParcel;
        parcelIndex++;

        SetParcelCollision(currentParcel, true);
        movement.SetTarget(currentParcel.transform.position);
        state = OperatorState.MovingToParcel;
    }

    private void CheckArrivalAtParcel()
    {
        if (currentParcel == null) return;
        Vector3 diff = transform.position - currentParcel.transform.position;
        float arriveDistSqr = GetArriveDistance() * GetArriveDistance();
        
        // Physically arrived OR the movement system confirms arrival at final target
        if (diff.x * diff.x + diff.z * diff.z < arriveDistSqr || movement.HasArrived)
        {
            OnArrivedAtParcel(currentParcel);
            state = OperatorState.Working;
        }
    }

    private void SetParcelCollision(EnvironmentalSensor parcel, bool ignore)
    {
        if (parcel == null) return;
        Collider col = parcel.GetComponent<Collider>();
        if (col != null) movement.IgnoreCollisionWith(col, ignore);
    }

    public string GetStatus()
    {
        if (energyManager != null && energyManager.IsCharging) return "Charging";
        return state switch
        {
            OperatorState.MovingToParcel => $"Moving to {(currentParcel ? currentParcel.name : "Parcel")}",
            OperatorState.Working => GetWorkingStatus(),
            OperatorState.Idle => GetIdleStatus(),
            _ => "Idle"
        };
    }

    // Subclass-specific hooks
    protected abstract float GetArriveDistance();
    protected abstract bool IsWorking();
    protected abstract void UpdateOperation();
    protected abstract void OnArrivedAtParcel(EnvironmentalSensor parcel);
    protected abstract void OnAllParcelsComplete();
    protected abstract void UpdateIdle();
    protected abstract string GetWorkingStatus();
    protected virtual string GetIdleStatus() => "Idle";
}
