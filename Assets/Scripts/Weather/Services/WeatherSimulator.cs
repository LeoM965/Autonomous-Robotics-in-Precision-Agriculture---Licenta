using UnityEngine;
using Weather.Models;
using System;

namespace Weather.Services
{
    public class WeatherSimulator
    {
        private ClimateProfile activeClimate;
        private bool initialized;

        // Smoothing: temperatura targetată vs. afișată
        private float targetTemperature = 20f;
        private float cachedWeatherOffset;
        private float cachedJitter;

        public WeatherType CurrentWeather { get; private set; } = WeatherType.Sunny;
        public float CurrentTemperature { get; set; } = 20f;
        public WeatherImpact CurrentImpact { get; private set; } = WeatherImpact.Get(WeatherType.Sunny);
        public WeatherType? ForcedWeather { get; set; }

        public void SetClimate(ClimateProfile profile) => activeClimate = profile;

        public void RerollWeather(float timeOfDay)
        {
            if (activeClimate == null) return;

            if (!initialized || UnityEngine.Random.value > activeClimate.persistenceFactor || ForcedWeather.HasValue)
            {
                CurrentWeather = RollNewWeather();
                CurrentImpact = WeatherImpact.Get(CurrentWeather);

                // Recalculăm offset-urile random doar la schimbarea vremii, nu la fiecare oră
                cachedWeatherOffset = UnityEngine.Random.Range(CurrentImpact.temperatureMin, CurrentImpact.temperatureMax);
                cachedJitter = UnityEngine.Random.Range(-activeClimate.temperatureVariability,
                                                        activeClimate.temperatureVariability) * activeClimate.jitterStrength;
                initialized = true;
            }

            UpdateTemperature(timeOfDay);
        }

        /// <summary>
        /// Apelat din Update() pentru smoothing continuu între orele simulate.
        /// </summary>
        public void SmoothTemperature(float lerpSpeed, bool snap = false)
        {
            if (snap)
                CurrentTemperature = targetTemperature;
            else
                CurrentTemperature = Mathf.Lerp(CurrentTemperature, targetTemperature, lerpSpeed);
        }

        private WeatherType RollNewWeather()
        {
            if (ForcedWeather.HasValue) return ForcedWeather.Value;

            float total = activeClimate.GetTotalWeight();
            if (total <= 0) return WeatherType.Sunny;

            float roll = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;

            foreach (WeatherType type in WeatherTypes.All)
            {
                cumulative += activeClimate.GetWeight(type);
                if (roll < cumulative) return type;
            }

            return WeatherType.Sunny;
        }

        public void UpdateTemperature(float timeOfDay)
        {
            if (activeClimate == null) return;

            float minTemp = activeClimate.minTemp;
            float maxTemp = activeClimate.maxTemp;

            // Ajustare dinamică pentru vară: temperaturi mai realiste (noapte răcoroasă, zi caldă)
            // prevenind temperaturi absurde de 30°C la primele ore ale dimineții
            if (activeClimate.seasonType == Season.Summer && Mathf.Approximately(minTemp, 25f))
            {
                minTemp = 16f;
                maxTemp = 32f;
            }

            float baseTemp = (minTemp + maxTemp) * 0.5f;
            float amplitude = (maxTemp - minTemp) * 0.5f;

            // Asymmetric diurnal cycle: minimum at 6:00 AM (sunrise), maximum at 3:00 PM (15:00)
            // Heating phase (6:00 - 15:00): 9 hours, with power curve for slower morning rise
            // Cooling phase (15:00 - 6:00 next day): 15 hours
            float diurnalOffset = 0f;
            if (timeOfDay >= 6f && timeOfDay <= 15f)
            {
                float t = (timeOfDay - 6f) / 9f;
                float tSlow = Mathf.Pow(t, 1.5f);
                diurnalOffset = -Mathf.Cos(tSlow * Mathf.PI) * amplitude;
            }
            else
            {
                float t = timeOfDay > 15f ? (timeOfDay - 15f) / 15f : (timeOfDay + 9f) / 15f;
                diurnalOffset = Mathf.Cos(t * Mathf.PI) * amplitude;
            }

            // Setăm target-ul — smoothing-ul real se face în SmoothTemperature()
            targetTemperature = baseTemp + diurnalOffset + cachedWeatherOffset + cachedJitter;
        }
    }
}

