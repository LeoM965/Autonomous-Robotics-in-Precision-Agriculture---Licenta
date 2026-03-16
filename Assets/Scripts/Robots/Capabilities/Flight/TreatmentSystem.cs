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

        public TreatmentSystem(Transform drone)
        {
            droneTransform = drone;
        }

        public void ApplyTreatment(EnvironmentalSensor target)
        {
            if (target == null) return;

            float speedAmount = TREATMENT_SPEED * Time.deltaTime;
            float nToAdd = speedAmount;
            float pToAdd = speedAmount * 0.5f;
            float kToAdd = speedAmount * 0.3f;

            var data = CropLoader.Load()?.Get(target.plantedVarietyName);
            if (data?.requirements?.nitrogen != null)
            {
                var reqs = data.requirements;
                
                // Calculate how much is missing up to the optimal level
                float missingN = Mathf.Max(0, reqs.nitrogen.optimal - target.nitrogen);
                float missingP = Mathf.Max(0, reqs.phosphorus.optimal - target.phosphorus);
                float missingK = Mathf.Max(0, reqs.potassium.optimal - target.potassium);

                // Calculate total missing to get proportions
                float totalMissing = missingN + missingP + missingK;

                if (totalMissing > 0)
                {
                    nToAdd = speedAmount * (missingN / totalMissing);
                    pToAdd = speedAmount * (missingP / totalMissing);
                    kToAdd = speedAmount * (missingK / totalMissing);
                }
            }
            
            // Limit spraying if it reached the target
            target.AdjustNutrients(nToAdd, pToAdd, kToAdd);
            LogDecision(target);
        }

        private void LogDecision(EnvironmentalSensor target)
        {
            if (DecisionTracker.Instance == null) return;

            var d = new DecisionRecord();
            d.decisionType = "Soil Treatment";
            d.chosenOption = "Pulverizing nutrients on " + target.name;
            d.parcelName = target.name;
            d.chosenScore = 100f;
            
            d.factors = new DecisionFactors
            {
                phScore = target.soilPH * 10f,
                humidityScore = target.soilMoisture,
                nitrogenScore = target.nitrogen / 10f,
                phosphorusScore = target.phosphorus / 10f,
                potassiumScore = target.potassium / 10f
            };

            DecisionTracker.Instance.RecordDecision(droneTransform, d);
        }
    }
}
