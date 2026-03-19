using UnityEngine;
using System.Collections.Generic;
using Robots.Components.Movement;
using Robots.Movement.Interfaces;

[RequireComponent(typeof(RobotPathfinder))]
[RequireComponent(typeof(RobotMotor))]
[RequireComponent(typeof(RobotWheelController))]
[RequireComponent(typeof(RobotLifecycle))]
public class RobotMovement : MonoBehaviour, IRobotMovement
{
    [Header("Wheels Configuration")]
    [SerializeField] private Transform[] wheels;
    [SerializeField] private float wheelRadius = 0.3f;

    [Header("Bounds")]
    [SerializeField] private float boundaryMargin = 12f;

    private RobotPathfinder pathfinder;
    private RobotMotor motor;
    private RobotWheelController wheelController;
    private Terrain terrain;
    private Rect movementBounds;

    public bool HasTarget => pathfinder != null && pathfinder.HasTarget;
    public bool HasArrived => pathfinder != null && pathfinder.HasArrived;
    public Vector3? FinalTarget => pathfinder?.FinalTarget;
    public List<Vector3> CurrentPath => pathfinder?.CurrentPath;

    private void Awake()
    {
        pathfinder = GetComponent<RobotPathfinder>();
        motor = GetComponent<RobotMotor>();
        wheelController = GetComponent<RobotWheelController>();
    }

    private void Start()
    {
        terrain = Terrain.activeTerrain;
        InitBounds();
        
        motor.Randomize(
            Random.Range(0.75f, 1.3f),
            Random.Range(0.8f, 1.2f),
            Random.Range(0.7f, 1.3f),
            Random.Range(0.8f, 1.2f),
            Random.Range(0.8f, 1.2f));
        
        float groundOffset = RobotHelper.CalculateGroundOffset(transform);
        motor.Initialize(terrain, movementBounds, groundOffset);
        
        wheelController.SetWheels(wheels, wheelRadius);
    }

    private void InitBounds()
    {
        FenceGenerator fence = FindFirstObjectByType<FenceGenerator>();
        if (fence?.zones != null)
        {
            FenceZone zone = BoundsHelper.FindZoneContaining(transform.position, fence.zones);
            if (zone == null && fence.zones.Length > 0)
                zone = fence.zones[0];
            if (zone != null)
            {
                movementBounds = BoundsHelper.GetZoneBounds(zone, boundaryMargin);
                return;
            }
        }
        if (terrain != null)
            movementBounds = BoundsHelper.GetTerrainBounds(terrain, boundaryMargin);
    }

    public void SetTerrain(Terrain t)
    {
        terrain = t;
        if (motor != null) motor.SetTerrain(t);
    }

    public void SetTarget(Vector3 target)
    {
        if (pathfinder != null) pathfinder.SetTarget(target);
        if (motor != null) motor.Resume();
    }

    public void ClearTarget()
    {
        if (pathfinder != null) pathfinder.ClearTarget();
    }

    public void Stop()
    {
        ClearTarget();
        if (motor != null) motor.Stop();
    }

    public void IgnoreCollisionWith(Collider target, bool ignore)
    {
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        foreach (Collider myCol in myColliders)
        {
            if (target != null && myCol != null)
                Physics.IgnoreCollision(myCol, target, ignore);
        }
    }
}
