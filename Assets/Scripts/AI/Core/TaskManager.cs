using UnityEngine;
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
        
        private readonly Dictionary<int, MinHeap<RobotTask>> zoneHeaps = new Dictionary<int, MinHeap<RobotTask>>();
        private readonly List<ITaskScanner> scanners = new List<ITaskScanner>();
        private FenceZone[] zones;
        private float scanTimer;

        public bool HasTasks => GetTotalTaskCount() > 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            InitializeZones();
            InitializeScanners();
        }

        private void InitializeZones()
        {
            FenceGenerator fenceGen = FindFirstObjectByType<FenceGenerator>();
            if (fenceGen != null && fenceGen.zones != null)
            {
                zones = fenceGen.zones;
                for (int i = 0; i < zones.Length; i++)
                    zoneHeaps[i] = new MinHeap<RobotTask>();
            }
        }

        private void InitializeScanners()
        {
            if (zones == null) return;
            scanners.Add(new SoilScanner(zones));
            scanners.Add(new HarvestScanner(zones));
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                ExecuteScanning();
                scanTimer = scanInterval;
            }
        }

        private void ExecuteScanning()
        {
            List<RobotTask> discoveredTasks = new List<RobotTask>();
            
            foreach (var scanner in scanners)
            {
                scanner.Scan(discoveredTasks);
            }

            foreach (var task in discoveredTasks)
            {
                EnqueueTask(task);
            }
        }

        private void EnqueueTask(RobotTask task)
        {
            int zoneIdx = GetTaskZoneIndex(task);
            if (zoneIdx >= 0 && zoneHeaps.ContainsKey(zoneIdx))
            {
                zoneHeaps[zoneIdx].Enqueue(task, task.Priority);
            }
        }

        private int GetTaskZoneIndex(RobotTask task)
        {
            var parcel = task.Target.GetComponent<EnvironmentalSensor>();
            return parcel != null ? parcel.zoneIndex : -1;
        }

        public RobotTask GetNextTask(int zoneIndex)
        {
            if (!zoneHeaps.TryGetValue(zoneIndex, out var heap) || heap.IsEmpty)
                return null;

            RobotTask task = heap.Dequeue();
            ResetParcelScheduling(task);
            return task;
        }

        private void ResetParcelScheduling(RobotTask task)
        {
            var parcel = task.Target.GetComponent<EnvironmentalSensor>();
            if (parcel != null) parcel.isScheduledForTask = false;
        }

        public RobotTask GetNextTask(Vector3 position)
        {
            FenceZone zone = BoundsHelper.FindZoneContaining(position, zones);
            if (zone == null) return null;
            return GetNextTask(System.Array.IndexOf(zones, zone));
        }

        private int GetTotalTaskCount()
        {
            int total = 0;
            foreach (var heap in zoneHeaps.Values) total += heap.Count;
            return total;
        }

        public void RegisterScanner(ITaskScanner scanner) => scanners.Add(scanner);
    }
}