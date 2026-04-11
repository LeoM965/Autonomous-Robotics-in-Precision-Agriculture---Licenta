using UnityEngine;
using System;
using System.Collections.Generic;
using Sensors.Components;
using AI.Core.Scanners;
using AI.DataStructures;

namespace AI.Core
{
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }

        [SerializeField] private float scanInterval = 5f;
        [SerializeField] private List<BaseScanner> activeScanners = new List<BaseScanner>();
        
        private readonly Dictionary<(Type, int), MinHeap<RobotTask>> taskHeaps = new();
        private FenceZone[] zones;
        private float scanTimer = 3f;
        private FenceGenerator cachedFenceGen;

        private void Awake()
        {
            if (Instance == null) 
            {
                Instance = this;
                EnvironmentalSensor.ResetAllScheduling();
            }
            else Destroy(gameObject);
        }

        private bool isInitialized = false;

        private void InitializeZones()
        {
            if (cachedFenceGen == null)
                cachedFenceGen = FindFirstObjectByType<FenceGenerator>();
            if (cachedFenceGen != null && cachedFenceGen.zones != null && cachedFenceGen.zones.Length > 0)
            {
                zones = cachedFenceGen.zones;
                isInitialized = true;
            }
        }

        private void Update()
        {
            if (!isInitialized)
            {
                InitializeZones();
                if (!isInitialized) return;
            }

            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                ExecuteScanning();
                scanTimer = scanInterval;
            }
        }

        private void ExecuteScanning()
        {
            if (activeScanners == null || activeScanners.Count == 0) return;

            var discovered = new List<RobotTask>();
            foreach (var scanner in activeScanners)
                if (scanner != null) scanner.Scan(discovered, zones);

            foreach (var task in discovered)
                EnqueueTask(task);
        }

        private void EnqueueTask(RobotTask task)
        {
            var parcel = task.Target.GetComponent<EnvironmentalSensor>();
            if (parcel == null) return;

            // Important: only set as scheduled if we successfully find a zone and enqueue it
            int zoneIdx = GetTaskZoneIndex(task);
            if (zoneIdx < 0) return;

            var key = (task.GetType(), zoneIdx);
            if (!taskHeaps.ContainsKey(key))
                taskHeaps[key] = new MinHeap<RobotTask>();

            taskHeaps[key].Enqueue(task, 1000f - task.NetValue);
            parcel.isScheduledForTask = true; 
        }

        private int GetTaskZoneIndex(RobotTask task)
        {
            var parcel = task.Target.GetComponent<EnvironmentalSensor>();
            return parcel != null ? parcel.zoneIndex : -1;
        }

        public T GetNextTask<T>(Vector3 position, bool allowCrossZone = true) where T : RobotTask
        {
            if (zones == null || zones.Length == 0) return null;

            // 1. Try Current Zone first (Minimizes travel time)
            FenceZone currentZone = BoundsHelper.FindZoneContaining(position, zones);
            if (currentZone == null && !allowCrossZone)
            {
                currentZone = BoundsHelper.FindClosestZone(position, zones);
            }

            if (currentZone != null)
            {
                int currentIdx = Array.IndexOf(zones, currentZone);
                var key = (typeof(T), currentIdx);
                if (taskHeaps.TryGetValue(key, out var heap) && !heap.IsEmpty)
                {
                    Debug.Log($"[TaskManager] Task gasit in zona curenta ({currentIdx}) pentru {typeof(T).Name}");
                    return heap.Dequeue() as T;
                }
            }

            if (!allowCrossZone) return null;

            // 2. Fallback: Search all other zones (Robots will travel to where the work is)
            for (int i = 0; i < zones.Length; i++)
            {
                var key = (typeof(T), i);
                if (taskHeaps.TryGetValue(key, out var heap) && !heap.IsEmpty)
                {
                    Debug.Log($"[TaskManager] Zona curenta goala. Redirectare robot catre Zona {i} pentru {typeof(T).Name}");
                    return heap.Dequeue() as T;
                }
            }

            return null;
        }

        private int GetTotalTaskCount()
        {
            int total = 0;
            foreach (var heap in taskHeaps.Values) total += heap.Count;
            return total;
        }
    }
}