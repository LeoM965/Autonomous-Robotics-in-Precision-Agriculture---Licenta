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

    protected virtual void Start() { }

    protected virtual void OnDisable()
    {
        ReleaseCurrentParcel();
    }

    protected virtual void Update()
    {
        if (energyManager == null) return;
        energyManager.Update();

        bool needsChargingAction = energyManager.IsHeadingToCharger || (energy != null && energy.IsCharging);

        if (needsChargingAction)
        {
            state = OperatorState.Charging;
        }
        else if (state == OperatorState.Charging)
        {
            state = OperatorState.Idle;
            MoveToNextParcel();
        }

        if (state != OperatorState.Charging)
            UpdateOperation();

        if (energy != null) 
        {
            energy.SetWorking(state == OperatorState.Working);
            energy.SetIdle(state == OperatorState.Idle);
        }

        switch (state)
        {
            case OperatorState.MovingToParcel:
                CheckArrivalAtParcel();
                break;
            case OperatorState.Working:
                if (!IsWorking()) MoveToNextParcel();
                break;
            case OperatorState.Idle:
                UpdateIdle();
                break;
        }
    }

    protected void MoveToNextParcel()
    {
        // Iterativ skip null parcels (evitam recursie infinita → StackOverflow)
        while (parcelIndex < parcels.Count && parcels[parcelIndex] == null)
        {
            parcelIndex++;
        }

        if (parcelIndex >= parcels.Count) 
        { 
            ReleaseCurrentParcel();
            OnAllParcelsComplete(); 
            return; 
        }

        EnvironmentalSensor nextParcel = parcels[parcelIndex];

        float dist = Vector3.Distance(transform.position, nextParcel.transform.position);
        if (energyManager != null && !energyManager.CheckBattery(dist, 60f)) 
        { 
            state = OperatorState.Charging; 
            return; 
        }

        SetParcelCollision(currentParcel, false);
        ReleaseCurrentParcel();
        currentParcel = nextParcel;
        parcelIndex++;

        SetParcelCollision(currentParcel, true);
        movement.SetTarget(currentParcel.transform.position);
        state = OperatorState.MovingToParcel;
    }

    protected void ReleaseCurrentParcel()
    {
        if (currentParcel != null)
        {
            currentParcel.isScheduledForTask = false;
            AbortOperation();
        }
    }

    private void CheckArrivalAtParcel()
    {
        if (currentParcel == null) return;
        Vector3 diff = transform.position - currentParcel.transform.position;
        float arriveDistSqr = GetArriveDistance() * GetArriveDistance();
        
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
        if (state == OperatorState.Charging)
        {
            return energyManager.IsHeadingToCharger ? "Going to Charger" : "Charging";
        }
        return state switch
        {
            OperatorState.MovingToParcel => $"Moving to {(currentParcel ? currentParcel.name : "Parcel")}",
            OperatorState.Working => GetWorkingStatus(),
            OperatorState.Idle => GetIdleStatus(),
            _ => "Idle"
        };
    }

    protected abstract float GetArriveDistance();
    protected abstract bool IsWorking();
    protected abstract void UpdateOperation();
    protected abstract void OnArrivedAtParcel(EnvironmentalSensor parcel);
    protected abstract void OnAllParcelsComplete();
    protected abstract void UpdateIdle();
    protected abstract string GetWorkingStatus();
    protected virtual string GetIdleStatus() => "Idle";
    protected abstract void AbortOperation();
}
