using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sensors.Components;
using Robots.Models;

public class AgroBotFlight : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private FlightSettings settings = new FlightSettings();
    [SerializeField] private Transform flightBody;

    private List<EnvironmentalSensor> _trackedParcels = new List<EnvironmentalSensor>();
    private EnvironmentalSensor _currentTargetParcel;
    private OperationRegion _region;
    private FlightState _state = FlightState.Initializing;
    
    private Vector3 _currentMoveTarget;
    private int _currentParcelIndex;
    private float _waitTimer;

    private Vector3 _lastPosition;

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegisterRobot();
        }
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.UnregisterRobot();
        }
    }

    private void Start()
    {
        if (flightBody == null) flightBody = transform;
        _lastPosition = flightBody.position;
        StartCoroutine(InitializationRoutine());
    }

    private IEnumerator InitializationRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        
        SetupOperationRegion();
        PopulateTrackedParcels();

        if (_trackedParcels.Count > 0)
        {
            _trackedParcels.Sort((a, b) => a.LatestAnalysis.qualityScore.CompareTo(b.LatestAnalysis.qualityScore));
            SetNextTarget();
            _state = FlightState.Navigating;
        }
        else StartCoroutine(InitializationRoutine());
    }

    private void SetupOperationRegion()
    {
        var fence = FindFirstObjectByType<FenceGenerator>();
        if (fence != null && fence.zones != null && fence.zones.Length > 0)
        {
            _region = OperationRegion.FromZone(GetNearestZone(fence.zones));
        }
        else
        {
            var terrain = Terrain.activeTerrain;
            Rect bounds = terrain != null 
                ? new Rect(0, 0, terrain.terrainData.size.x, terrain.terrainData.size.z)
                : new Rect(-1000, -1000, 2000, 2000);
            _region = new OperationRegion(bounds);
        }
    }

    private FenceZone GetNearestZone(FenceZone[] zones)
    {
        float minSqrDist = float.MaxValue;
        int bestIndex = 0;
        Vector3 pos = transform.position;

        for (int i = 0; i < zones.Length; i++)
        {
            Vector2 center = (zones[i].startXZ + zones[i].endXZ) * 0.5f;
            float sqrDist = (pos.x - center.x) * (pos.x - center.x) + (pos.z - center.y) * (pos.z - center.y);
            if (sqrDist < minSqrDist) { minSqrDist = sqrDist; bestIndex = i; }
        }
        return zones[bestIndex];
    }

    private void PopulateTrackedParcels()
    {
        _trackedParcels = _region.FilterParcels(ParcelCache.Parcels);
        if (_trackedParcels.Count == 0) _trackedParcels.AddRange(ParcelCache.Parcels);
    }

    private void Update()
    {
        if (_state == FlightState.Initializing) return;

        UpdateHoverPhysics();

        // Track distance for simulation time passage
        float distMoved = Vector3.Distance(flightBody.position, _lastPosition);
        if (distMoved > 0.001f && TimeManager.Instance != null)
        {
            TimeManager.Instance.AddDistanceTraveled(distMoved);
        }
        _lastPosition = flightBody.position;

        if (_state == FlightState.Navigating) ExecuteNavigation();
        else ExecuteHoverWait();
    }

    private void ExecuteNavigation()
    {
        Vector3 pos = flightBody.position;
        Vector3 dir = _currentMoveTarget - pos;
        dir.y = 0;

        if (dir.magnitude > 0.1f)
        {
            Vector3 moveDir = dir.normalized;
            flightBody.position += moveDir * settings.speed * Time.deltaTime;
            flightBody.rotation = Quaternion.Slerp(flightBody.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 3f);
        }

        if (dir.sqrMagnitude < settings.ArrivalThresholdSqr)
        {
            _waitTimer = settings.waitTimePerParcel;
            _state = FlightState.HoveringAtTarget;
        }
    }

    private void ExecuteHoverWait()
    {
        _waitTimer -= Time.deltaTime;
        if (_waitTimer <= 0f)
        {
            SetNextTarget();
            _state = FlightState.Navigating;
        }
    }

    private void UpdateHoverPhysics()
    {
        Vector3 pos = flightBody.position;
        pos.y = settings.altitude + Mathf.Sin(Time.time * settings.AngularHoverFrequency) * settings.hoverAmplitude;
        
        pos.x = Mathf.Clamp(pos.x, _region.Bounds.xMin - 5f, _region.Bounds.xMax + 5f);
        pos.z = Mathf.Clamp(pos.z, _region.Bounds.yMin - 5f, _region.Bounds.yMax + 5f);
        
        flightBody.position = pos;
    }

    private void SetNextTarget()
    {
        if (_trackedParcels.Count == 0) return;
        _currentParcelIndex = (_currentParcelIndex + 1) % _trackedParcels.Count;
        _currentTargetParcel = _trackedParcels[_currentParcelIndex];
        _currentMoveTarget = _currentTargetParcel.transform.position;
        _currentMoveTarget.y = settings.altitude;
    }

    public string GetStatus() => _currentTargetParcel ? $"{_currentTargetParcel.name} [{_state}]" : "Idle";
}
