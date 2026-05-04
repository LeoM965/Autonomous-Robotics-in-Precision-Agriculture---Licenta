using System;
using System.Collections.Generic;

namespace SaveSystem
{
    /// <summary>
    /// Serializable data containers for simulation state persistence.
    /// Saved as JSON to Application.persistentDataPath.
    /// </summary>
    [Serializable]
    public class SimSaveData
    {
        public string saveName;
        public string savedAt;
        public float totalSimulatedHours;
        public int dayNumber;

        // Weather
        public string weatherType;
        public float temperature;

        // Economics
        public float globalEnergyCost;
        public float globalMaintenanceCost;
        public float globalDepreciationCost;

        // Daily history snapshots
        public List<Economics.Models.DailySnapshot> dailyHistory = new List<Economics.Models.DailySnapshot>();

        public List<ParcelSave> parcels = new List<ParcelSave>();
        public List<RobotSave> robots = new List<RobotSave>();
    }

    [Serializable]
    public class ParcelSave
    {
        public string name;
        public float moisture;
        public float pH;
        public float nitrogen;
        public float phosphorus;
        public float potassium;
        public float irrigationRate;
        public string plantedVariety;
        public int harvestedCount;
        public float harvestedWeightKg;
        public float harvestedRevenue;
        public float harvestedSeedCost;

        // Individual crops with exact positions
        public List<CropSave> crops = new List<CropSave>();
    }

    [Serializable]
    public class CropSave
    {
        public float posX, posY, posZ;
        public float rotY;
        public float progress;
        public float purchasePrice;
    }

    [Serializable]
    public class RobotSave
    {
        public string name;
        public float posX, posY, posZ;
        public float rotY;
        public float batteryKWh;

        // Per-robot economics
        public float distance;
        public float time;
        public float energykWh;
        public float maintenanceCost;
        public float depreciationCost;
        public float revenueGenerated;

        // Decision history
        public List<AI.Analytics.DecisionRecord> decisions = new List<AI.Analytics.DecisionRecord>();
    }
}
