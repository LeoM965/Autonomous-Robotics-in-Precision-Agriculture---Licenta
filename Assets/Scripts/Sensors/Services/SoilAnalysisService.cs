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
            // Asymmetric: penalty is 2.5x harsher for over-saturation than for dryness
            score += ScoreFactor(soil.moisture, s.moistureOptimal, s.moistureTolerance, 2.5f) * s.moistureWeight;
            score += NutrientScore(soil.nitrogen, s.nCritical, s.nOptimal) * s.nitrogenWeight;
            score += NutrientScore(soil.phosphorus, s.pCritical, s.pOptimal) * s.phosphorusWeight;
            score += NutrientScore(soil.potassium, s.kCritical, s.kOptimal) * s.potassiumWeight;
            
            return Mathf.Clamp(score, 0f, 100f);
        }

        private static float ScoreFactor(float value, float optimal, float tolerance, float overTolerancePenalty = 1.0f)
        {
            if (tolerance <= 0.001f) tolerance = 1.0f;
            float deviation = value - optimal;
            
            // Base leniency multiplier (3.0x from previous fix)
            float effectivizer = 3.0f;
            
            // If we are ABOVE the optimal (e.g. flooding), we apply the penalty multiplier
            if (deviation > 0) effectivizer /= overTolerancePenalty;

            return Mathf.Clamp01(1f - Mathf.Abs(deviation) / (tolerance * effectivizer));
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
