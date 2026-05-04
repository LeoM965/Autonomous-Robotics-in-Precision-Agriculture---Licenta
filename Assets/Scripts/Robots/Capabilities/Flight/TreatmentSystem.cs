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

        public void ProcessTreatment(EnvironmentalSensor target, ref float timer)
        {
            if (target == null) { timer = 0; activeRecord = null; return; }

            var data = CropLoader.Load()?.Get(target.plantedVarietyName);
            float requiredN = data?.requirements?.nitrogen?.optimal ?? 100f;

            if (target.nitrogen < requiredN)
            {
                ApplyTreatment(target, data);
                timer -= Time.deltaTime;
            }
            else timer = 0;
        }

        private void ApplyTreatment(EnvironmentalSensor target, CropData data)
        {
            float speed = TREATMENT_SPEED * Time.deltaTime;
            float nToAdd = speed, pToAdd = speed * 0.5f, kToAdd = speed * 0.3f;

            if (data?.requirements?.nitrogen != null)
            {
                var reqs = data.requirements;
                float mN = Mathf.Max(0, reqs.nitrogen.optimal - target.nitrogen);
                float mP = Mathf.Max(0, reqs.phosphorus.optimal - target.phosphorus);
                float mK = Mathf.Max(0, reqs.potassium.optimal - target.potassium);
                float total = mN + mP + mK;

                if (total > 0)
                {
                    nToAdd = speed * (mN / total);
                    pToAdd = speed * (mP / total);
                    kToAdd = speed * (mK / total);
                }
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

            activeRecord = new DecisionRecord
            {
                decisionType = "Soil Treatment",
                chosenOption = "Treat Soil",
                parcelName = target.name,
                chosenScore = urgency,
                schedulingValue = priority,
                netValue = urgency - energyCost,
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
