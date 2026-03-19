using UnityEngine;
using System.Collections.Generic;

public static class RobotDataLoader
{
    private static RobotDatabase database;

    public static RobotDatabase Load()
    {
        if (database != null) return database;
        TextAsset json = Resources.Load<TextAsset>("RobotData");
        if (json == null) return null;
        database = JsonUtility.FromJson<RobotDatabase>(json.text);
        return database;
    }

    public static RobotDataEntry FindByName(string robotName)
    {
        var db = Load();
        if (db?.robots == null) return null;

        string searchName = robotName;
        
        foreach (var entry in db.robots)
        {
            if (searchName.StartsWith(entry.namePrefix)) return entry;
        }
        return null;
    }
}

[System.Serializable]
public class RobotDatabase
{
    public List<RobotDataEntry> robots;
}

[System.Serializable]
public class RobotDataEntry
{
    public string namePrefix;
    public string model;
    public float purchasePrice;
    public float maxSpeed;
    public float batteryCapacity;
    public float weightKg;
    public float maintenanceRate;
    public int utilityLifeYears;
    public float residualValueRate;
    public float consumptionMeter;
    public float consumptionWorkSec;
    public float consumptionStandbySec;
    public float rechargeRate;
}
