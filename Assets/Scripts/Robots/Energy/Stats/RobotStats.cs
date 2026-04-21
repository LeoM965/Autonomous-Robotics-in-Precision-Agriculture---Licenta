using UnityEngine;
using Settings;

public class RobotStats
{
    public float distance;
    public float time;
    public float speed;
    public string type;
    public string model;
    public float purchasePrice;
    public float maintenanceRate;
    public int utilityLifeYears;
    public float residualValueRate;
    public float maintenanceCost;
    public float depreciationCost;
    public float energykWh;
    public float revenueGenerated;
    public string zone = "?";
    public bool IsIdle { get; set; }
    
    public static float EnergyPrice => SimulationSettings.EnergyPrice;
    public float TotalCost => maintenanceCost + depreciationCost + (energykWh * EnergyPrice);
    public float ROI 
    {
        get {
            float basePrice = purchasePrice > 0 ? purchasePrice : 100000f; // Fallback if data missing
            return ((revenueGenerated - TotalCost) / basePrice) * 100f;
        }
    }

    public RobotStats(Transform robot)
    {
        RobotDataEntry data = RobotDataLoader.FindByName(robot.name);
        if (data != null)
        {
            purchasePrice = data.purchasePrice;
            model = data.model;
            maintenanceRate = data.maintenanceRate;
            utilityLifeYears = data.utilityLifeYears;
            residualValueRate = data.residualValueRate;
        }
        else
        {
            Debug.LogWarning($"[RobotStats] No data in RobotData.json for '{robot.name}'");
        }

        type = DetectType(robot.name, data?.namePrefix);
        UpdateZone(robot.position); // Inițializăm zona la creare
    }

    public float AddMaintenance(float distMeters, float deltaHours)
    {
        if (IsIdle) return 0f;

        float deltaKm = distMeters / 1000f;
        float annualBudget = purchasePrice * maintenanceRate;
        float costDist = (deltaKm * (annualBudget * 0.5f)) / 10000f;
        float costTime = (deltaHours * (annualBudget * 0.5f)) / 1500f;
        float added = costDist + costTime;

        distance += distMeters;
        maintenanceCost += added;

        if (deltaHours > 0)
            speed = distMeters / (deltaHours * 3600f);

        return added;
    }

    public float AddDepreciation(float deltaHours)
    {
        if (deltaHours <= 0 || IsIdle) return 0;

        float annualDep = purchasePrice * (1f - residualValueRate);
        int life = utilityLifeYears > 0 ? utilityLifeYears : 10;
        float hourlyDep = annualDep / (life * 365f * 24f); 
        
        float added = hourlyDep * deltaHours;
        depreciationCost += added;
        return added;
    }

    public void AddEnergy(float kWh)
    {
        energykWh += kWh;
    }

    public void AddRevenue(float amount)
    {
        revenueGenerated += amount;
    }

    public void UpdateZone(Vector3 pos)
    {
        float minDistSqr = float.MaxValue;
        var parcels = ParcelCache.Parcels;
        if (parcels == null || parcels.Count == 0) return;

        foreach (var p in parcels)
        {
            if (p == null) continue;
            float dSqr = (pos - p.transform.position).sqrMagnitude;
            if (dSqr < minDistSqr)
            {
                minDistSqr = dSqr;
                zone = GetZoneFromName(p.name);
            }
        }
    }

    private string GetZoneFromName(string name)
    {
        int i = name.IndexOf('_');
        if (i >= 0 && i + 1 < name.Length)
            return name[i + 1].ToString().ToUpper();
        return "?";
    }

    private static string DetectType(string name, string prefix)
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            return prefix;
        }

        if (name.Contains("HarvestBot")) return "HarvestBot";
        if (name.Contains("AgroBot")) return "AgroBot";
        if (name.Contains("AgBot")) return "AgBot";

        return name.Replace("(Clone)", "").Trim();
    }
}
