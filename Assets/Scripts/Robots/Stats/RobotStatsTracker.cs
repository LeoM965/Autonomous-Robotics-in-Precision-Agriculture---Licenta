using UnityEngine;
using System.Collections.Generic;
public class RobotStatsTracker
{
    private Dictionary<Transform, RobotStats> statsMap = new Dictionary<Transform, RobotStats>();
    public float TotalDistance { get; private set; }
    public float TotalCost { get; private set; }
    public void Track(List<GameObject> robots, float deltaTime)
    {
        TotalDistance = 0f;
        TotalCost = 0f;
        for (int i = 0; i < robots.Count; i++)
        {
            GameObject robot = robots[i];
            if (robot == null)
                continue;
            Transform t = robot.transform;
            RobotStats stats = GetOrCreate(t);
            stats.Update(t.position, deltaTime);
            TotalDistance += stats.distance;
            TotalCost += stats.GetOperatingCost();
        }
    }
    private RobotStats GetOrCreate(Transform robot)
    {
        RobotStats stats;
        if (statsMap.TryGetValue(robot, out stats))
            return stats;
        stats = new RobotStats(robot);
        statsMap[robot] = stats;
        return stats;
    }
    public RobotStats Get(Transform robot)
    {
        RobotStats stats;
        if (statsMap.TryGetValue(robot, out stats))
            return stats;
        return null;
    }
    public bool Has(Transform robot)
    {
        return statsMap.ContainsKey(robot);
    }
}
