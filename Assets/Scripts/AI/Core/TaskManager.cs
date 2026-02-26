using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Sensors.Models;
using Sensors.Services;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }
    [SerializeField] private float scanInterval = 5f;
    private Dictionary<int, MinHeap<RobotTask>> _zoneHeaps = new Dictionary<int, MinHeap<RobotTask>>();
    private FenceZone[] _zones;
    private float _scanTimer;
    public bool HasTasks
    {
        get { return GetTotalTaskCount() > 0; }
    }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        FenceGenerator fenceGen = FindFirstObjectByType<FenceGenerator>();
        if (fenceGen != null && fenceGen.zones != null)
        {
            _zones = fenceGen.zones;
            for (int i = 0; i < _zones.Length; i++)
                _zoneHeaps[i] = new MinHeap<RobotTask>();
        }
    }
    private void Update()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            ScanParcels();
            ScanHarvestableParcels();
            _scanTimer = scanInterval;
        }
    }
    private void ScanParcels()
    {
        if (ParcelCache.Instance == null) return;
        
        foreach (var parcel in ParcelCache.Instance.ParcelsIterator)
        {
            if (parcel == null || parcel.composition == null || parcel.isScheduledForTask)
                continue;
            
            SoilAnalysis analysis = parcel.LatestAnalysis;
            if (analysis.HasAlerts)
            {
                if (parcel.zoneIndex == -1)
                {
                    FenceZone zone = BoundsHelper.FindZoneContaining(parcel.transform.position, _zones);
                    if (zone != null) parcel.zoneIndex = System.Array.IndexOf(_zones, zone);
                }
                
                int zoneIdx = parcel.zoneIndex;
                if (zoneIdx >= 0 && _zoneHeaps.ContainsKey(zoneIdx))
                {
                    TaskType type = GetTaskType(analysis);
                    _zoneHeaps[zoneIdx].Enqueue(new RobotTask(parcel.transform, type, analysis.qualityScore), analysis.qualityScore);
                    parcel.isScheduledForTask = true;
                }
            }
        }
    }
    private TaskType GetTaskType(SoilAnalysis analysis)
    {
        if (analysis.requiresIrrigation)
            return TaskType.Irrigate;
        if (analysis.requiresFertilization)
            return TaskType.Fertilize;
        if (analysis.requiresLiming)
            return TaskType.Lime;
        return TaskType.Scout;
    }
    private void ScanHarvestableParcels()
    {
        if (ParcelCache.Instance == null) return;
        foreach (var parcel in ParcelCache.Instance.ParcelsIterator)
        {
            if (parcel == null || parcel.isScheduledForTask || parcel.activeCrops.Count == 0) continue;
            
            int matureCount = 0;
            foreach (var crop in parcel.activeCrops)
            {
                if (crop != null && crop.IsFullyGrown) matureCount++;
            }
            
            if (matureCount == 0) continue;
            
            if (parcel.zoneIndex == -1)
            {
                FenceZone zone = BoundsHelper.FindZoneContaining(parcel.transform.position, _zones);
                if (zone != null) parcel.zoneIndex = System.Array.IndexOf(_zones, zone);
            }
            
            int zoneIdx = parcel.zoneIndex;
            if (zoneIdx < 0 || !_zoneHeaps.ContainsKey(zoneIdx)) continue;
            
            float priority = matureCount * 10f;
            _zoneHeaps[zoneIdx].Enqueue(new RobotTask(parcel.transform, TaskType.Harvest, priority), priority);
            parcel.isScheduledForTask = true;
        }
    }
    public RobotTask GetNextTask(int zoneIndex)
    {
        if (!_zoneHeaps.ContainsKey(zoneIndex))
            return null;
        MinHeap<RobotTask> heap = _zoneHeaps[zoneIndex];
        if (heap.IsEmpty)
            return null;
        
        RobotTask task = heap.Dequeue();
        var parcel = task.Target.GetComponent<EnvironmentalSensor>();
        if (parcel != null)
            parcel.isScheduledForTask = false;
            
        return task;
    }
    public RobotTask GetNextTask(Vector3 position)
    {
        FenceZone zone = BoundsHelper.FindZoneContaining(position, _zones);
        if (zone == null) return null;
        int zoneIdx = System.Array.IndexOf(_zones, zone);
        return GetNextTask(zoneIdx);
    }
    private int GetTotalTaskCount()
    {
        int total = 0;
        foreach (MinHeap<RobotTask> heap in _zoneHeaps.Values)
            total += heap.Count;
        return total;
    }
}
