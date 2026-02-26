using UnityEngine;

namespace Economics.Services
{
    public static class CapExEngine
    {
        public static float CalculateTotalInvestment(float purchasePrice, int count)
        {
            return purchasePrice * count;
        }

        public static float CalculateAnnualDepreciation(float totalInvestment, int usefulLifeYears)
        {
            if (usefulLifeYears <= 0) return 0f;
            return totalInvestment / usefulLifeYears;
        }
    }
}
