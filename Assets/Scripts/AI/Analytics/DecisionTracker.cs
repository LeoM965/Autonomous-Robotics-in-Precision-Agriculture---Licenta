using UnityEngine;
using System.Collections.Generic;

public class DecisionTracker : MonoBehaviour
{
    public static DecisionTracker Instance { get; private set; }

    private Dictionary<Transform, DecisionRecord> lastDecisions = new Dictionary<Transform, DecisionRecord>();
    private Dictionary<Transform, List<DecisionRecord>> decisionHistory = new Dictionary<Transform, List<DecisionRecord>>();
    private Dictionary<Transform, float> totalScores = new Dictionary<Transform, float>();
    private readonly List<Transform> toRemove = new List<Transform>();

    [SerializeField] private int maxHistoryPerRobot = 50;
    [SerializeField] private float cleanupInterval = 10f;
    private float nextCleanupTime;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
        if (robot == null || record == null)
            return;

        record.timestamp = Time.time;
        lastDecisions[robot] = record;

        if (!decisionHistory.ContainsKey(robot))
        {
            decisionHistory[robot] = new List<DecisionRecord>();
            totalScores[robot] = 0f;
        }

        List<DecisionRecord> history = decisionHistory[robot];
        history.Add(record);
        totalScores[robot] += record.chosenScore;

        if (history.Count > maxHistoryPerRobot)
        {
            totalScores[robot] -= history[0].chosenScore;
            history.RemoveAt(0);
        }
    }

    public DecisionRecord GetLastDecision(Transform robot)
    {
        DecisionRecord record;
        if (lastDecisions.TryGetValue(robot, out record))
            return record;
        return null;
    }

    public int GetTotalDecisions(Transform robot)
    {
        List<DecisionRecord> history;
        if (decisionHistory.TryGetValue(robot, out history))
            return history.Count;
        return 0;
    }

    public float GetAverageScore(Transform robot)
    {
        List<DecisionRecord> history;
        float total;
        if (!decisionHistory.TryGetValue(robot, out history) || history.Count == 0)
            return 0f;
        if (!totalScores.TryGetValue(robot, out total))
            return 0f;
        return total / history.Count;
    }

    private void CleanupDestroyedRobots()
    {
        toRemove.Clear();

        foreach (var key in lastDecisions.Keys)
        {
            if (key == null)
                toRemove.Add(key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            lastDecisions.Remove(toRemove[i]);
            decisionHistory.Remove(toRemove[i]);
            totalScores.Remove(toRemove[i]);
        }
    }
}
