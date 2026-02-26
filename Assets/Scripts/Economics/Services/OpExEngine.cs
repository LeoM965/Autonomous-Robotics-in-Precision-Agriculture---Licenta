namespace Economics.Services
{
    public static class OpExEngine
    {
        public static float CalculateAnnualOperatingCost(float hourlyCost, float annualHours, int count)
        {
            return hourlyCost * annualHours * count;
        }

        public static float CalculateAnnualMaintenance(float annualMaintenancePerUnit, int count)
        {
            return annualMaintenancePerUnit * count;
        }
    }
}
