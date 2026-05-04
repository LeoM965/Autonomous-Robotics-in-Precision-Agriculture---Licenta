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

    // Anti-stuck: forțează arrival dacă robotul e blocat mai mult de 8s
    private float moveTimer;
    private const float MAX_MOVE_TIME = 8f;

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

        // Oprire forțată pe vreme rea
        if (SimulationSpeedController.Instance != null &&
            SimulationSpeedController.Instance.ShouldRobotsStop)
        {
            ForceIdle();
            SyncIdleToManager();
            return;
        }

        // Cedează dacă alt operator de pe același robot e ocupat
        if (state == OperatorState.Idle && AnotherOperatorIsActive())
        {
            SyncIdleToManager();
            return;
        }

        energyManager.Update();

        bool needsCharge = energyManager.IsHeadingToCharger ||
                           (energy != null && energy.IsCharging);

        if (needsCharge)
            state = OperatorState.Charging;
        else if (state == OperatorState.Charging)
        {
            state = OperatorState.Idle;
            MoveToNextParcel();
        }

        if (state != OperatorState.Charging)
            UpdateOperation();

        if (energy != null)
            energy.SetWorking(state == OperatorState.Working);

        SyncIdleToManager();

        switch (state)
        {
            case OperatorState.MovingToParcel:
                moveTimer += Time.deltaTime;
                if (moveTimer > MAX_MOVE_TIME && currentParcel != null)
                {
                    // Blocat prea mult — consideră că a ajuns
                    OnArrivedAtParcel(currentParcel);
                    state = OperatorState.Working;
                }
                else
                {
                    CheckArrivalAtParcel();
                }
                break;

            case OperatorState.Working:
                if (!IsWorking()) MoveToNextParcel();
                break;

            case OperatorState.Idle:
                UpdateIdle();
                break;
        }
    }

    // ── Helpers ──

    private bool AnotherOperatorIsActive()
    {
        var ops = GetComponents<RobotOperator>();
        foreach (var op in ops)
        {
            if (op != this && op.state != OperatorState.Idle)
                return true;
        }
        return false;
    }

    private void SyncIdleToManager()
    {
        if (energy == null) return;

        var ops = GetComponents<RobotOperator>();
        bool allIdle = true;
        foreach (var op in ops)
        {
            if (op.state != OperatorState.Idle)
            { allIdle = false; break; }
        }
        energy.SetIdle(allIdle);
    }

    private void ForceIdle()
    {
        if (state == OperatorState.Idle) return;
        ReleaseCurrentParcel();
        movement.Stop();
        state = OperatorState.Idle;
    }

    // ── Navigation ──

    protected void MoveToNextParcel()
    {
        while (parcelIndex < parcels.Count && parcels[parcelIndex] == null)
            parcelIndex++;

        if (parcelIndex >= parcels.Count)
        {
            ReleaseCurrentParcel();
            OnAllParcelsComplete();
            return;
        }

        EnvironmentalSensor next = parcels[parcelIndex];
        float dist = Vector3.Distance(transform.position, next.transform.position);

        if (energyManager != null && !energyManager.CheckBattery(dist, 60f))
        {
            state = OperatorState.Charging;
            return;
        }

        SetParcelCollision(currentParcel, false);
        ReleaseCurrentParcel();

        currentParcel = next;
        parcelIndex++;
        moveTimer = 0f;

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
        float r = GetArriveDistance();

        if (diff.x * diff.x + diff.z * diff.z < r * r || movement.HasArrived)
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

    // ── Status ──

    public string GetStatus()
    {
        if (state == OperatorState.Charging)
            return energyManager.IsHeadingToCharger ? "Going to Charger" : "Charging";

        return state switch
        {
            OperatorState.MovingToParcel => $"Moving to {(currentParcel ? currentParcel.name : "Parcel")}",
            OperatorState.Working        => GetWorkingStatus(),
            OperatorState.Idle           => GetIdleStatus(),
            _                            => "Idle"
        };
    }

    // ── Abstract ──

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
