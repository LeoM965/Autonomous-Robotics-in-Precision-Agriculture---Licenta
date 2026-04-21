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
    [Header("Bounds")]
    [SerializeField] private float boundaryMargin = 12f;

    private RobotPathfinder pathfinder;
    private RobotMotor motor;
    private RobotWheelController wheelController;
    private Terrain terrain;
    private Rect movementBounds;
    private FenceGenerator cachedFence;

    public bool HasTarget => pathfinder != null && pathfinder.HasTarget;
    public bool HasArrived => pathfinder != null && pathfinder.HasArrived;
    public Vector3? FinalTarget => pathfinder?.FinalTarget;
    public List<Vector3> CurrentPath => pathfinder?.CurrentPath;

    private Collider[] cachedColliders;

    private void Awake()
    {
        pathfinder = GetComponent<RobotPathfinder>();
        motor = GetComponent<RobotMotor>();
        wheelController = GetComponent<RobotWheelController>();
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private bool isInitialized = false;

    private void Start()
    {
        motor.Randomize(
            Random.Range(0.75f, 1.3f),
            Random.Range(0.8f, 1.2f),
            Random.Range(0.7f, 1.3f),
            Random.Range(0.8f, 1.2f),
            Random.Range(0.8f, 1.2f));
    }

    private void Update()
    {
        if (!isInitialized)
        {
            terrain = Terrain.activeTerrain;
            if (cachedFence == null)
                cachedFence = FenceGenerator.Instance;
            if (terrain != null && cachedFence != null && cachedFence.zones != null && cachedFence.zones.Length > 0)
            {
                InitBounds();
                float groundOffset = RobotHelper.CalculateGroundOffset(transform);
                motor.Initialize(terrain, movementBounds, groundOffset);
                isInitialized = true;
            }
        }
    }

    public void InitBounds()
    {
        if (cachedFence == null)
            cachedFence = FenceGenerator.Instance;
        if (cachedFence?.zones != null)
        {
            FenceZone zone = BoundsHelper.FindZoneContaining(transform.position, cachedFence.zones);
            if (zone == null && cachedFence.zones.Length > 0)
                zone = cachedFence.zones[0];
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
        if (cachedColliders == null) return;
        foreach (Collider myCol in cachedColliders)
        {
            if (target != null && myCol != null && myCol.gameObject.activeInHierarchy)
                Physics.IgnoreCollision(myCol, target, ignore);
        }
    }
}
