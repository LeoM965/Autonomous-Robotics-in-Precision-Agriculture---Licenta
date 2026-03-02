using UnityEngine;
using Economics.Services;
public class RobotStats
{
    public float distance;
    public float time;
    public float speed;
    public string type;
    public float hourlyCost;
    public float purchasePrice;
    private Vector3 lastPosition;
    public RobotStats(Transform robot)
    {
        type = DetectType(robot.name);
        var robotData = EconomicDataLoader.GetRobot(type);
        hourlyCost = 10f;
        purchasePrice = 50000f;
        if (robotData != null)
        {
            hourlyCost = robotData.hourlyCostEUR;
            purchasePrice = robotData.purchaseCostEUR;
        }
        lastPosition = robot.position;
    }
    public void Update(Vector3 currentPosition, float deltaTime)
    {
        float moved = Vector3.Distance(currentPosition, lastPosition);
        speed = moved / deltaTime;
        distance += moved;
        time += deltaTime;
        lastPosition = currentPosition;
    }
    public float GetOperatingCost()
    {
        float hours = time / 3600f;
        return hours * hourlyCost;
    }

    private static string DetectType(string name)
    {
        if (name.StartsWith("AgroBot")) return "AgroBot";
        if (name.StartsWith("AgBot")) return "AgBot";
        if (name.Contains("HarvestBot")) return "HarvestBot";
        if (name.Contains("Hybrid")) return "AgBot"; // Using AgBot stats for hybrid
        return name;
    }
}
