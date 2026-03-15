using UnityEngine;
using System.Collections.Generic;
using Economics.Models;

namespace Economics.Managers
{
    public class RobotEconomicsManager : MonoBehaviour
    {
        public static RobotEconomicsManager Instance;

        public Dictionary<Transform, RobotStats> RobotStatsMap { get; private set; } = new Dictionary<Transform, RobotStats>();
        
        public float GlobalEnergyCost => globalEnergykWh * RobotStats.EnergyPrice;
        public float GlobalMaintenanceCost { get; private set; }
        public float GlobalDepreciationCost { get; private set; }

        private float globalEnergykWh;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            ScanForExistingRobots();
        }

        private float lastSyncTotalHours = -1f;

        private void ScanForExistingRobots()
        {
            RobotMovement[] movements = FindObjectsByType<RobotMovement>(FindObjectsSortMode.None);
            foreach (var m in movements)
                RegisterRobot(m.transform);
        }

        private void LateUpdate()
        {
            if (TimeManager.Instance == null) return;

            if (lastSyncTotalHours < 0)
            {
                lastSyncTotalHours = TimeManager.Instance.TotalSimulatedHours;
                return;
            }

            float worldDelta = TimeManager.Instance.TotalSimulatedHours - lastSyncTotalHours;
            lastSyncTotalHours = TimeManager.Instance.TotalSimulatedHours;

            if (worldDelta > 0)
            {
                foreach (var stats in RobotStatsMap.Values)
                {
                    if (stats.IsCurrentlyActive)
                    {
                        stats.time += worldDelta * 3600f;
                        stats.IsCurrentlyActive = false; // Reset pentru frame-ul următor
                    }
                }
            }
        }

        private void Update()
        {
            if (Time.frameCount % 60 == 0)
                CleanUpDestroyedRobots();
        }

        public void RecordStatus(Transform robot, float kWh, float distMeters, float deltaHours)
        {
            if (!RobotStatsMap.ContainsKey(robot)) RegisterRobot(robot);
            
            var stats = RobotStatsMap[robot];
            stats.IsCurrentlyActive = true;
            
            globalEnergykWh += kWh;
            stats.AddEnergy(kWh);

            if (Time.frameCount % 30 == 0)
                stats.UpdateZone(robot.position);
            
            if (distMeters > 0 || deltaHours > 0)
            {
                GlobalMaintenanceCost += stats.AddMaintenance(distMeters, deltaHours);
                GlobalDepreciationCost += stats.AddDepreciation(deltaHours);
            }
        }

        public void AddRobotRevenue(Transform robot, float amount)
        {
            if (robot == null || amount <= 0) return;
            if (!RobotStatsMap.ContainsKey(robot)) RegisterRobot(robot);
            RobotStatsMap[robot].AddRevenue(amount);
        }

        public void RegisterRobot(Transform robot)
        {
            if (!RobotStatsMap.ContainsKey(robot))
            {
                RobotStatsMap[robot] = new RobotStats(robot);
                Debug.Log($"[Economics] Inregistrat robot: {robot.name}");
            }
        }

        private readonly List<Transform> toRemove = new List<Transform>();

        private void CleanUpDestroyedRobots()
        {
            toRemove.Clear();
            foreach (var robot in RobotStatsMap.Keys)
                if (robot == null) toRemove.Add(robot);
            foreach (var r in toRemove) RobotStatsMap.Remove(r);
        }

    }
}
