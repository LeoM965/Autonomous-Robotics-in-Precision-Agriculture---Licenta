using UnityEngine;
using Sensors.Models;

namespace Sensors.Services
{
    public static class SoilAnalysisService
    {
        public static SoilAnalysis Analyze(SoilComposition composition, SoilSettings settings)
        {
            float quality = CalculateQuality(composition, settings);
            
            return new SoilAnalysis
            {
                qualityScore = quality,
                health = DetermineHealth(quality, settings),
                requiresIrrigation = composition.moisture < settings.moistureMin,
                requiresFertilization = composition.nitrogen < settings.nCritical 
                                     || composition.phosphorus < settings.pCritical 
                                     || composition.potassium < settings.kCritical,
                requiresLiming = composition.pH < settings.phMin,
                requiresAcidification = composition.pH > settings.phMax
            };
        }

        public static float CalculateQuality(SoilComposition soil, SoilSettings s)
        {
            float score = 0f;
            score += ScoreFactor(soil.pH, s.phIdeal, s.phTolerance) * s.phWeight;
            score += ScoreFactor(soil.moisture, s.moistureOptimal, s.moistureTolerance) * s.moistureWeight;
            score += NutrientScore(soil.nitrogen, s.nCritical, s.nOptimal) * s.nitrogenWeight;
            score += NutrientScore(soil.phosphorus, s.pCritical, s.pOptimal) * s.phosphorusWeight;
            score += NutrientScore(soil.potassium, s.kCritical, s.kOptimal) * s.potassiumWeight;
            
            return Mathf.Clamp(score, 0f, 100f);
        }

        private static float ScoreFactor(float value, float optimal, float tolerance)
        {
            if (tolerance <= 0.001f) tolerance = 1.0f; // Prevent div by zero
            float deviation = Mathf.Abs(value - optimal);
            return Mathf.Clamp01(1f - deviation / (tolerance * 1.5f));
        }

        private static float NutrientScore(float value, float low, float optimal)
        {
            if (low <= 0.001f) low = 1.0f; // Prevent div by zero
            if (optimal <= low) optimal = low + 1.0f;

            if (value < low) return (value / low) * 0.5f;
            return 0.5f + Mathf.Clamp01((value - low) / (optimal - low)) * 0.5f;
        }

        private static SoilHealthStatus DetermineHealth(float score, SoilSettings settings)
        {
            if (score >= settings.excellentThreshold) return SoilHealthStatus.Excellent;
            if (score >= settings.optimalThreshold) return SoilHealthStatus.Optimal;
            if (score >= settings.suboptimalThreshold) return SoilHealthStatus.Suboptimal;
            if (score >= settings.deficientThreshold) return SoilHealthStatus.Deficient;
            return SoilHealthStatus.Critical;
        }
    }
}
