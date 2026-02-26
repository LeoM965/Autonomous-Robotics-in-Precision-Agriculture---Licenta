using UnityEngine;

public class RobotEnergyManager
{
    private RobotEnergy energy;
    private RobotMovement movement;
    private Transform transform;
    private Vector3? currentChargerTarget;
    private float chargingTimer;
    private bool isCharging;

    public RobotEnergyManager(Transform t, RobotEnergy e, RobotMovement m)
    {
        transform = t;
        energy = e;
        movement = m;
    }

    public bool IsCharging => isCharging;
    public float ChargingTimer => chargingTimer;

    public bool CheckBattery(float distance, float estimatedWorkSeconds)
    {
        if (energy != null && !energy.HasEnoughEnergy(distance, estimatedWorkSeconds))
        {
            GoToCharger();
            return false;
        }
        return true;
    }

    private void GoToCharger()
    {
        Vector3? station = BuildingSpawner.GetNearestChargingStation(transform.position);
        if (station.HasValue)
        {
            currentChargerTarget = station.Value;
            movement.SetTarget(station.Value);
            isCharging = true;
        }
        else isCharging = false;
    }

    public void Update()
    {
        if (!isCharging) return;
        if (currentChargerTarget.HasValue) CheckArrivalAtCharger();
        else CheckChargingStatus();
    }

    private void CheckArrivalAtCharger()
    {
        if (!currentChargerTarget.HasValue) return;
        Vector3 diff = transform.position - currentChargerTarget.Value;
        if (diff.x * diff.x + diff.z * diff.z < 25f || !movement.HasTarget)
        {
            movement.Stop();
            if (energy != null) energy.StartCharging();
            chargingTimer = 5.0f;
            currentChargerTarget = null;
        }
    }

    private void CheckChargingStatus()
    {
        chargingTimer -= Time.deltaTime;
        if (chargingTimer <= 0f)
        {
            if (energy != null) energy.SetFullBattery();
            isCharging = false;
        }
    }
}
