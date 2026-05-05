using UnityEngine;
using Sensors.Components;
using AI.Analytics;
using AI.Models.Decisions;

namespace Robots.Capabilities.Flight
{
    public class TreatmentSystem
    {
        private const float TREATMENT_SPEED = 25f;
        private Transform droneTransform;
        private EnvironmentalSensor lastLoggedParcel;
        private DecisionRecord activeRecord;
        private FlightNavigation navigation;

        public TreatmentSystem(Transform drone, FlightNavigation nav)
        {
            droneTransform = drone;
            navigation = nav;
        }

        /// <summary>
        /// Processes soil treatment on the target parcel.
        /// Treats ALL deficient nutrients (N, P, K) — not just nitrogen.
        /// Uses the same 0.95 threshold as FlightNavigation.NeedsTreatment for consistency.
        /// </summary>
        public void ProcessTreatment(EnvironmentalSensor target, ref float timer)
        {
            if (target == null) { timer = 0; activeRecord = null; return; }

            var data = CropLoader.Load()?.Get(target.plantedVarietyName);
            float optN = data?.requirements?.nitrogen?.optimal ?? 100f;
            float optP = data?.requirements?.phosphorus?.optimal ?? 50f;
            float optK = data?.requirements?.potassium?.optimal ?? 50f;

            // Check if ANY nutrient needs treatment (consistent 0.95 threshold)
            bool needsN = target.nitrogen < optN * 0.95f;
            bool needsP = target.phosphorus < optP * 0.95f;
            bool needsK = target.potassium < optK * 0.95f;

            if (needsN || needsP || needsK)
            {
                ApplyTreatment(target, data, optN, optP, optK);
                timer -= Time.deltaTime;
            }
            else
            {
                // All nutrients are at or above 95% optimal — treatment complete
                timer = 0;
            }
        }

        private void ApplyTreatment(EnvironmentalSensor target, CropData data,
                                     float optN, float optP, float optK)
        {
            float speed = TREATMENT_SPEED * Time.deltaTime;

            // Calculate deficit for each nutrient
            float mN = Mathf.Max(0, optN - target.nitrogen);
            float mP = Mathf.Max(0, optP - target.phosphorus);
            float mK = Mathf.Max(0, optK - target.potassium);
            float total = mN + mP + mK;

            float nToAdd, pToAdd, kToAdd;
            if (total > 0)
            {
                // Distribute treatment proportionally to each nutrient's deficit
                nToAdd = speed * (mN / total);
                pToAdd = speed * (mP / total);
                kToAdd = speed * (mK / total);
            }
            else
            {
                nToAdd = 0; pToAdd = 0; kToAdd = 0;
            }

            target.AdjustNutrients(nToAdd, pToAdd, kToAdd);

            if (target != lastLoggedParcel)
            {
                LogDecision(target);
                lastLoggedParcel = target;
            }

            UpdateLiveFactors(target);
        }

        private void LogDecision(EnvironmentalSensor target)
        {
            if (DecisionTracker.Instance == null) return;

            float urgency = navigation?.LastUrgency ?? 0f;
            float dist = navigation?.LastDistance ?? 0f;
            float priority = urgency / Mathf.Max(dist, 1f);
            float energyCost = dist * 0.001f;

            // Score = soil satisfaction (100 = fully nourished, 0 = fully depleted)
            // More intuitive than raw urgency which is inversely correlated with soil health
            float satisfaction = 100f - urgency;

            activeRecord = new DecisionRecord
            {
                decisionType = "Soil Treatment",
                chosenOption = "Treat Soil",
                parcelName = target.name,
                chosenScore = satisfaction,
                schedulingValue = priority,
                netValue = satisfaction - energyCost,
                factors = new DecisionFactors()
            };

            if (navigation != null)
            {
                var alts = navigation.GetTopAlternatives(3);
                foreach (var (parcel, altUrg, altDist) in alts)
                    activeRecord.alternatives.Add(new DecisionAlternative(parcel.name, altUrg));
            }

            UpdateLiveFactors(target);
            DecisionTracker.Instance.RecordDecision(droneTransform, activeRecord);
        }

        private void UpdateLiveFactors(EnvironmentalSensor target)
        {
            if (activeRecord?.factors == null) return;

            var data = CropLoader.Load()?.Get(target.plantedVarietyName);
            float optN = data?.requirements?.nitrogen?.optimal ?? 100f;
            float optP = data?.requirements?.phosphorus?.optimal ?? 50f;
            float optK = data?.requirements?.potassium?.optimal ?? 50f;

            activeRecord.factors.phScore = Mathf.Clamp(target.soilPH / 7f * 100f, 0f, 100f);
            activeRecord.factors.humidityScore = Mathf.Clamp(target.soilMoisture, 0f, 100f);
            activeRecord.factors.nitrogenScore = Mathf.Clamp(target.nitrogen / optN * 100f, 0f, 100f);
            activeRecord.factors.phosphorusScore = Mathf.Clamp(target.phosphorus / optP * 100f, 0f, 100f);
            activeRecord.factors.potassiumScore = Mathf.Clamp(target.potassium / optK * 100f, 0f, 100f);
        }
    }
}
