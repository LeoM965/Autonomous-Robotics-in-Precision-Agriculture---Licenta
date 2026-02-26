using UnityEngine;
using Sensors.Models;

namespace Sensors.Services
{
    public static class SoilAnalysisService
    {
        // Agronomic constants (Academic thresholds)
        public const float N_OPTIMAL = 480f;
        public const float P_OPTIMAL = 22f;
        public const float K_OPTIMAL = 280f;
        public const float PH_IDEAL = 6.5f;
        public const float MOISTURE_MIN = 35f;

        public static SoilAnalysis Analyze(SoilComposition composition)
        {
            float quality = CalculateQuality(composition);
            
            return new SoilAnalysis
            {
                qualityScore = quality,
                health = DetermineHealth(quality),
                requiresIrrigation = composition.moisture < MOISTURE_MIN,
                requiresFertilization = composition.nitrogen < 240f || composition.phosphorus < 11f || composition.potassium < 110f,
                requiresLiming = composition.pH < 5.5f,
                requiresAcidification = composition.pH > 7.8f,
                
                nitrogenDeficit = Mathf.Max(0, N_OPTIMAL - composition.nitrogen),
                phosphorusDeficit = Mathf.Max(0, P_OPTIMAL - composition.phosphorus),
                potassiumDeficit = Mathf.Max(0, K_OPTIMAL - composition.potassium),
                moistureDeficit = Mathf.Max(0, MOISTURE_MIN - composition.moisture)
            };
        }

        public static float CalculateQuality(SoilComposition soil)
        {
            float score = 0f;
            score += ScoreFactor(soil.pH, PH_IDEAL, 1.5f) * 30f;
            score += ScoreFactor(soil.moisture, 55f, 25f) * 25f;
            score += NutrientScore(soil.nitrogen, 240f, N_OPTIMAL) * 20f;
            score += NutrientScore(soil.phosphorus, 11f, P_OPTIMAL) * 15f;
            score += NutrientScore(soil.potassium, 110f, K_OPTIMAL) * 10f;
            
            return Mathf.Clamp(score, 0f, 100f);
        }

        private static float ScoreFactor(float value, float optimal, float tolerance)
        {
            float deviation = Mathf.Abs(value - optimal);
            return Mathf.Clamp01(1f - deviation / (tolerance * 1.5f));
        }

        private static float NutrientScore(float value, float low, float optimal)
        {
            if (value < low) return (value / low) * 0.5f;
            return 0.5f + Mathf.Clamp01((value - low) / (optimal - low)) * 0.5f;
        }

        private static SoilHealthStatus DetermineHealth(float score)
        {
            if (score >= 85) return SoilHealthStatus.Excellent;
            if (score >= 65) return SoilHealthStatus.Optimal;
            if (score >= 45) return SoilHealthStatus.Suboptimal;
            if (score >= 25) return SoilHealthStatus.Deficient;
            return SoilHealthStatus.Critical;
        }
    }
}
