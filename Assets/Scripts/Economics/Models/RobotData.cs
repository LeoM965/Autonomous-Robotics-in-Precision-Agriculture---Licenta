using System;
namespace Economics.Models
{
    [Serializable]
    public class RobotData
    {
        public string id;
        public string name;
        public float purchaseCostEUR;
        public float hourlyCostEUR;
        public float maintenanceAnnualEUR;
        public float usefulLifeYears;
    }
}
