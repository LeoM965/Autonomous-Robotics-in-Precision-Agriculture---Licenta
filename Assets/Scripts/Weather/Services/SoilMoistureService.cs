using UnityEngine;
using Sensors.Components;
using Weather.Models;
using System.Collections.Generic;

namespace Weather.Services
{
    public static class SoilMoistureService
    {
        public static void UpdateMoisture(IEnumerable<EnvironmentalSensor> parcels, WeatherImpact impact, ClimateProfile climate, float deltaHours)
        {
            if (deltaHours <= 0f || climate == null) return;

            var beforeVals = new Dictionary<EnvironmentalSensor, float>();
            foreach (var p in parcels)
            {
                if (p != null && p.composition != null) 
                    beforeVals[p] = p.composition.moisture;
            }

            // Subdivide large time gaps (SkipDay/SkipMonth) into 1h chunks
            while (deltaHours > 1.0f)
            {
                ApplyMoistureStep(parcels, impact, climate, 1.0f);
                deltaHours -= 1.0f;
            }
            if (deltaHours > 0.001f)
                ApplyMoistureStep(parcels, impact, climate, deltaHours);

            foreach (var p in parcels)
            {
                if (p == null || p.composition == null) continue;
                if (beforeVals.TryGetValue(p, out float before))
                {
                    if (Mathf.Abs(before - p.composition.moisture) > 0.5f)
                        p.Analyze();
                }
            }
        }

        private static void ApplyMoistureStep(IEnumerable<EnvironmentalSensor> parcels, WeatherImpact impact, ClimateProfile climate, float deltaHours)
        {
            float evapRate = climate.evaporationRate / 24f;
            float precipRate = impact.precipitationRate;

            foreach (var parcel in parcels)
            {
                if (parcel == null || parcel.composition == null) continue;

                float moisture = parcel.composition.moisture;
                float absorption = precipRate * (1f - moisture / 100f);
                float evaporation = evapRate * (moisture / 100f);
                float drainage = 0.3f * Mathf.Max(0f, (moisture - 60f) / 40f);

                float netChange = (absorption - evaporation - drainage) * deltaHours;
                parcel.composition.moisture = Mathf.Clamp(moisture + netChange, 0f, 100f);
            }
        }
    }
}
