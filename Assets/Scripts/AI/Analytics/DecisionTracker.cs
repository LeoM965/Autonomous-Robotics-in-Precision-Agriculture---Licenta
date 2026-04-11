using UnityEngine;
using System.Collections.Generic;
using AI.Models.Decisions;

namespace AI.Analytics
{
    public class DecisionTracker : MonoBehaviour
    {
        public static DecisionTracker Instance { get; private set; }

        [SerializeField] private int maxHistoryPerRobot = 1000;
        [SerializeField] private float cleanupInterval = 10f;

        private readonly Dictionary<Transform, DecisionRecord> lastDecisions = new Dictionary<Transform, DecisionRecord>();
        private readonly Dictionary<Transform, List<DecisionRecord>> decisionHistory = new Dictionary<Transform, List<DecisionRecord>>();
        private readonly Dictionary<Transform, float> totalScores = new Dictionary<Transform, float>();
        private readonly Dictionary<Transform, int> totalDecisionsCount = new Dictionary<Transform, int>();
        private readonly List<Transform> toRemove = new List<Transform>();
        private float nextCleanupTime;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            if (Time.time >= nextCleanupTime)
            {
                CleanupDestroyedRobots();
                nextCleanupTime = Time.time + cleanupInterval;
            }
        }

        public void RecordDecision(Transform robot, DecisionRecord record)
        {
            if (robot == null || record == null) return;

            record.timestamp = TimeManager.Instance != null 
                ? TimeManager.Instance.TotalSimulatedHours 
                : Time.time;
            lastDecisions[robot] = record;

            if (!decisionHistory.ContainsKey(robot))
            {
                decisionHistory[robot] = new List<DecisionRecord>();
                totalScores[robot] = 0f;
                totalDecisionsCount[robot] = 0;
            }

            List<DecisionRecord> history = decisionHistory[robot];
            record.globalIndex = totalDecisionsCount[robot] + 1;
            history.Add(record);
            totalScores[robot] += record.chosenScore;
            totalDecisionsCount[robot]++;

            if (history.Count > maxHistoryPerRobot)
            {
                totalScores[robot] -= history[0].chosenScore;
                history.RemoveAt(0);
            }
        }

        public DecisionRecord GetLastDecision(Transform robot)
        {
            return lastDecisions.TryGetValue(robot, out var record) ? record : null;
        }

        public int GetTotalDecisions(Transform robot)
        {
            return totalDecisionsCount.TryGetValue(robot, out var count) ? count : 0;
        }

        public float GetAverageScore(Transform robot)
        {
            if (!decisionHistory.TryGetValue(robot, out var history) || history.Count == 0)
                return 0f;
            
            return totalScores[robot] / history.Count;
        }

        public List<DecisionRecord> GetRecentDecisions(Transform robot, int count = 10)
        {
            if (!decisionHistory.TryGetValue(robot, out var history) || history.Count == 0)
                return new List<DecisionRecord>();

            int start = Mathf.Max(0, history.Count - count);
            return history.GetRange(start, history.Count - start);
        }

        private void CleanupDestroyedRobots()
        {
            toRemove.Clear();
            foreach (var robot in lastDecisions.Keys)
                if (robot == null) toRemove.Add(robot);
            foreach (var robot in toRemove)
            {
                lastDecisions.Remove(robot);
                decisionHistory.Remove(robot);
                totalScores.Remove(robot);
                totalDecisionsCount.Remove(robot);
            }
        }
    }
}