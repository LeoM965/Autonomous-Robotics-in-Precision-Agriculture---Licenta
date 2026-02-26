using UnityEngine;
namespace Economics.Services
{
    public static class FinancialIndicators
    {
        public const float DEFAULT_DISCOUNT_RATE = 0.08f;

        public static float ROI(float laborSavings, float opEx, float maintenance, float depreciation, float investment)
        {
            if (investment <= 0) return 0f;
            float netBenefit = laborSavings - opEx - maintenance - depreciation;
            return (netBenefit / investment) * 100f;
        }

        public static float NPV(float investment, float annualCashFlow, int years, float discountRate = DEFAULT_DISCOUNT_RATE)
        {
            float npv = -investment;
            for (int year = 1; year <= years; year++)
            {
                float factor = Mathf.Pow(1f + discountRate, year);
                npv += annualCashFlow / factor;
            }
            return npv;
        }

        public static float Payback(float investment, float annualNetGain)
        {
            if (annualNetGain <= 0) return 999f;
            return investment / annualNetGain;
        }

        public static float CashFlow(float laborSavings, float opEx, float maintenance)
        {
            return laborSavings - opEx - maintenance;
        }
    }
}
