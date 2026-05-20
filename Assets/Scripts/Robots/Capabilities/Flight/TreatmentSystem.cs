using UnityEngine;
using Sensors.Components;
using AI.Analytics;
using AI.Models.Decisions;

namespace Robots.Capabilities.Flight
{
    public class TreatmentSystem
    {
        private const float TREATMENT_SPEED = 25f;
        private const float MAX_SUBSTEP = 0.02f;
        private Transform droneTransform;
        private EnvironmentalSensor lastLoggedParcel;
        private DecisionRecord activeRecord;
        private FlightNavigation navigation;

        public TreatmentSystem(Transform drone, FlightNavigation nav)
        {
            droneTransform = drone;
            navigation = nav;
        }

        public void ResetSession()
        {
            lastLoggedParcel = null;
            activeRecord = null;
        }


        /// <summary>
        /// Processes soil treatment on the target parcel.
        /// Treats ALL deficient nutrients (N, P, K) — not just nitrogen.
        /// Uses the same 0.80 threshold as FlightNavigation.NeedsTreatment for consistency.
        /// </summary>
        public void ProcessTreatment(EnvironmentalSensor target, ref float timer)
        {
            if (target == null) { timer = 0; activeRecord = null; return; }

            var data = CropLoader.Load()?.Get(target.plantedVarietyName);
            float optN = data?.requirements?.nitrogen?.optimal ?? 100f;
            float optP = data?.requirements?.phosphorus?.optimal ?? 50f;
            float optK = data?.requirements?.potassium?.optimal ?? 50f;
            float optPH = data?.requirements?.pH?.optimal ?? 6.5f;
            float optM = data?.requirements?.humidity?.optimal ?? 60f;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            int steps = Mathf.CeilToInt(dt / MAX_SUBSTEP);
            float stepDt = dt / steps;

            for (int s = 0; s < steps; s++)
            {
                bool needsN = target.nitrogen < optN;
                bool needsP = target.phosphorus < optP;
                bool needsK = target.potassium < optK;
                bool needsPH = Mathf.Abs(target.soilPH - optPH) > 0.05f;
                bool needsM = target.soilMoisture < optM;

                if (needsN || needsP || needsK || needsPH || needsM)
                {
                    ApplyTreatment(target, data, optN, optP, optK, optPH, optM, stepDt);
                    timer -= stepDt;
                }
                else
                {
                    timer = 0;
                    break;
                }
            }
        }

        private void ApplyTreatment(EnvironmentalSensor target, CropData data,
                                     float optN, float optP, float optK, float optPH, float optM,
                                     float stepDt)
        {
            float speed = TREATMENT_SPEED * stepDt;

            // Calculate deficit for each nutrient + moisture
            float mN = Mathf.Max(0, optN - target.nitrogen);
            float mP = Mathf.Max(0, optP - target.phosphorus);
            float mK = Mathf.Max(0, optK - target.potassium);
            float mPH = Mathf.Abs(optPH - target.soilPH) * 20f; // Scale pH diff to be comparable
            float mMoist = Mathf.Max(0, optM - target.soilMoisture);
            float total = mN + mP + mK + mPH + mMoist;

            float nToAdd, pToAdd, kToAdd, phToAdd, moistToAdd;
            if (total > 0)
            {
                // Distribute treatment proportionally to each deficit
                nToAdd = speed * (mN / total);
                pToAdd = speed * (mP / total);
                kToAdd = speed * (mK / total);
                phToAdd = speed * (mPH / total) * 0.05f; // Rescale back to pH scale
                moistToAdd = speed * (mMoist / total);
                
                if (target.soilPH > optPH) phToAdd = -phToAdd;
            }
            else
            {
                nToAdd = 0; pToAdd = 0; kToAdd = 0; phToAdd = 0; moistToAdd = 0;
            }

            if (target != lastLoggedParcel)
            {
                LogDecision(target, optN, optP, optK, optPH, optM);
                lastLoggedParcel = target;
            }

            // Clamp values to prevent overshooting the optimal values
            float finalN = Mathf.Min(nToAdd, Mathf.Max(0, optN - target.nitrogen));
            float finalP = Mathf.Min(pToAdd, Mathf.Max(0, optP - target.phosphorus));
            float finalK = Mathf.Min(kToAdd, Mathf.Max(0, optK - target.potassium));
            float finalM = Mathf.Min(moistToAdd, Mathf.Max(0, optM - target.soilMoisture));
            
            float finalPH = phToAdd;
            if (phToAdd > 0) finalPH = Mathf.Min(phToAdd, optPH - target.soilPH);
            else if (phToAdd < 0) finalPH = Mathf.Max(phToAdd, optPH - target.soilPH);

            target.AdjustNutrients(finalN, finalP, finalK);
            if (Mathf.Abs(finalPH) > 0.001f) target.AdjustPH(finalPH);
            if (finalM > 0.001f) target.AdjustMoisture(finalM);

            // Accumulate applied amounts in the active record
            if (activeRecord != null)
            {
                activeRecord.appliedN += finalN;
                activeRecord.appliedP += finalP;
                activeRecord.appliedK += finalK;
                activeRecord.appliedPH += finalPH;
                activeRecord.appliedMoisture += finalM;
            }

            UpdateLiveFactors(target);
        }

        private void LogDecision(EnvironmentalSensor target, float optN, float optP, float optK, float optPH, float optM)
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
                factors = new DecisionFactors(),

                // Fertilization details
                cropVariety = target.plantedVarietyName ?? "",
                initialN = target.nitrogen,
                initialP = target.phosphorus,
                initialK = target.potassium,
                initialPH = target.soilPH,
                initialMoisture = target.soilMoisture,
                appliedN = 0f, appliedP = 0f, appliedK = 0f, appliedPH = 0f, appliedMoisture = 0f,
                optimalN = optN, optimalP = optP, optimalK = optK, optimalPH = optPH, optimalMoisture = optM
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

