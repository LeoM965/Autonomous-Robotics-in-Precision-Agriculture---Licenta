using UnityEngine;
using Economics.Models;

namespace Economics.Services
{
    public static class FinancialEngine
    {
        public static FinancialReport GenerateReport(string robotId, int unitCount = 1)
        {
            var robot = EconomicDataLoader.GetRobot(robotId);
            var labor = EconomicDataLoader.GetLabor();

            if (robot == null || labor == null)
                return new FinancialReport { isValid = false };

            float annualHours = labor.workHoursPerMonth * 12f;
            float totalCapEx = CapExEngine.CalculateTotalInvestment(robot.purchaseCostEUR, unitCount);
            float annualDepreciation = CapExEngine.CalculateAnnualDepreciation(totalCapEx, (int)robot.usefulLifeYears);

            float annualOpEx = OpExEngine.CalculateAnnualOperatingCost(robot.hourlyCostEUR, annualHours, unitCount);
            float annualMaintenance = OpExEngine.CalculateAnnualMaintenance(robot.maintenanceAnnualEUR, unitCount);

            float annualLaborSavings = labor.costPerHourRomania * labor.workersReplacedByRobot * annualHours * unitCount;
            
            float annualCashFlow = FinancialIndicators.CashFlow(annualLaborSavings, annualOpEx, annualMaintenance);
            float roi = FinancialIndicators.ROI(annualLaborSavings, annualOpEx, annualMaintenance, annualDepreciation, totalCapEx);
            float npv = FinancialIndicators.NPV(totalCapEx, annualCashFlow, (int)robot.usefulLifeYears);
            float payback = FinancialIndicators.Payback(totalCapEx, annualCashFlow);

            return new FinancialReport
            {
                isValid = true,
                robotName = robot.name,
                unitCount = unitCount,
                totalInvestment = totalCapEx,
                annualDepreciation = annualDepreciation,
                annualOperatingCost = annualOpEx,
                annualMaintenanceCost = annualMaintenance,
                annualLaborSavings = annualLaborSavings,
                annualCashFlow = annualCashFlow,
                annualROI = roi,
                netPresentValue = npv,
                paybackPeriodYears = payback,
                annualOperatingHours = annualHours,
                usefulLifeYears = (int)robot.usefulLifeYears
            };
        }
    }
}
