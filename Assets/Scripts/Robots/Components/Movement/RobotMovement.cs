using UnityEngine;
using System.Collections.Generic;

public partial class RobotMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float rotationSpeed = 120f;
    public float heightSpeed = 10f;
    public float tiltSpeed = 3f;
    public float maxTilt = 20f;

    [Header("Wheels")]
    [SerializeField] Transform[] wheels;
    [SerializeField] float wheelRadius = 0.3f;

    [Header("Avoidance")]
    [SerializeField] float avoidRadius = 2f;
    [SerializeField] float boundaryMargin = 12f;

    float targetAngle, currentAngle, wheelRotation, currentHeight, groundOffset;
    float currentPitch, currentRoll;
    Rect movementBounds;
    Vector3 lastFixedPos, lastUpdatePos, velocity, groundNormal = Vector3.up;
    Vector3[] wheelAngles;
    List<Vector3> path;
    int pathIndex;
    Vector3? finalTarget;
    Terrain terrain;
    bool isStopped;
    float stuckTimer;
    Vector3 stuckPushDir;

    const float WAYPOINT_THRESHOLD = 0.8f;
    const float ARRIVAL_THRESHOLD = 0.5f;

    public static List<RobotMovement> allRobots = new List<RobotMovement>();

    void OnEnable()
    {
        allRobots.Add(this);
        if (TimeManager.Instance != null)
            TimeManager.Instance.RegisterRobot();
    }

    void OnDisable()
    {
        allRobots.Remove(this);
        if (TimeManager.Instance != null)
            TimeManager.Instance.UnregisterRobot();
    }

    void Start()
    {
        RandomizeParameters();
        currentAngle = targetAngle = transform.eulerAngles.y;
        lastFixedPos = lastUpdatePos = transform.position;
        terrain = Terrain.activeTerrain;
        groundOffset = RobotHelper.CalculateGroundOffset(transform);
        if (terrain != null)
            currentHeight = TerrainHelper.GetHeight(transform.position) + groundOffset;
        InitWheels();
        InitBounds();
    }

    void InitBounds()
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

    void RandomizeParameters()
    {
        speed = Random.Range(speed * 0.75f, speed * 1.3f);
        rotationSpeed = Random.Range(rotationSpeed * 0.8f, rotationSpeed * 1.2f);
        tiltSpeed = Random.Range(tiltSpeed * 0.7f, tiltSpeed * 1.3f);
        maxTilt = Random.Range(maxTilt * 0.8f, maxTilt * 1.2f);
        avoidRadius = Random.Range(avoidRadius * 0.8f, avoidRadius * 1.2f);
    }

    public void SetTerrain(Terrain t) => terrain = t;

    public void SetTarget(Vector3 target)
    {
        isStopped = false;
        finalTarget = target;
        stuckTimer = 0f;
        stuckPushDir = Vector3.zero;
        RequestPath(target);
    }

    public void ClearTarget()
    {
        path = null;
        finalTarget = null;
        pathIndex = 0;
    }

    public void Stop()
    {
        ClearTarget();
        velocity = Vector3.zero;
        isStopped = true;
    }

    public bool HasTarget => path != null && pathIndex < path.Count;
    public bool HasArrived => finalTarget.HasValue && Vector3.Distance(transform.position, finalTarget.Value) < ARRIVAL_THRESHOLD;
    public Vector3? FinalTarget => finalTarget;
    public List<Vector3> CurrentPath => path;

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
