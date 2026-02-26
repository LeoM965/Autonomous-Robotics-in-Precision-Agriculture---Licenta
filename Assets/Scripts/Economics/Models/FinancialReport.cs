using System;
using UnityEngine;

namespace Economics.Models
{
    [Serializable]
    public class FinancialReport
    {
        public bool isValid;
        public string robotName;
        public int unitCount;

        [Header("Capital Expenditure (CapEx)")]
        public float totalInvestment;
        public float annualDepreciation;
        
        [Header("Operating Expenditure (OpEx)")]
        public float annualOperatingCost;
        public float annualMaintenanceCost;
        
        [Header("Returns & Efficiency")]
        public float annualLaborSavings;
        public float annualCashFlow;
        public float annualROI;
        public float netPresentValue;
        public float paybackPeriodYears;
        
        [Header("Context")]
        public float annualOperatingHours;
        public int usefulLifeYears;

        public bool IsInvestmentRecovered => paybackPeriodYears > 0 && paybackPeriodYears <= usefulLifeYears;
    }
}
