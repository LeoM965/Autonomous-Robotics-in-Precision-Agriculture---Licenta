using UnityEngine;
using Robots.Movement.Interfaces;

public class RobotEnergyManager
{
    private RobotEnergy energy;
    private IRobotMovement movement;
    private Transform transform;
    private Vector3? currentChargerTarget;
    private bool isHeadingToCharger;

    public RobotEnergyManager(Transform t, RobotEnergy e, IRobotMovement m)
    {
        transform = t;
        energy = e;
        movement = m;
    }

    public bool IsHeadingToCharger => isHeadingToCharger;

    public bool CheckBattery(float distance, float estimatedWorkSeconds)
    {
        if (energy != null && !energy.HasEnoughEnergy(distance, estimatedWorkSeconds))
        {
            StartNavigationToCharger();
            return false;
        }
        return true;
    }

    private void StartNavigationToCharger()
    {
        Vector3? station = BuildingSpawner.GetNearestChargingStation(transform.position);
        if (station.HasValue)
        {
            currentChargerTarget = station.Value;
            movement.SetTarget(station.Value);
            isHeadingToCharger = true;
        }
        else 
        {
            isHeadingToCharger = false;
            currentChargerTarget = null;
        }
    }

    public void Update()
    {
        if (isHeadingToCharger && currentChargerTarget.HasValue) 
            CheckArrivalAtCharger();
    }

    private void CheckArrivalAtCharger()
    {
        if (!currentChargerTarget.HasValue) return;

        float sqrDist = (transform.position - currentChargerTarget.Value).sqrMagnitude;
        bool hasReachedStation = sqrDist < 36f || movement.HasArrived || !movement.HasTarget;

        if (hasReachedStation)
        {
            movement.Stop();
            if (energy != null) energy.StartCharging();
            isHeadingToCharger = false;
            currentChargerTarget = null;
        }
    }
}
