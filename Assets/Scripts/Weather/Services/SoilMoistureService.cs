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

            // Protection: ignore large time gaps (jumps/skips) to keep soil values stable
            if (deltaHours > 1.0f) return;

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
                float before = moisture;
                parcel.composition.moisture = Mathf.Clamp(moisture + netChange, 0f, 100f);

                if (Mathf.Abs(before - parcel.composition.moisture) > 1.0f)
                    parcel.Analyze();
            }
        }
    }
}
