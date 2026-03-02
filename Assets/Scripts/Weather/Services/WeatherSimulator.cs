using UnityEngine;
using Weather.Models;
using System;

namespace Weather.Services
{
    public class WeatherSimulator
    {
        private ClimateProfile activeClimate;
        private bool initialized;

        public WeatherType CurrentWeather { get; private set; }
        public float CurrentTemperature { get; private set; }
        public WeatherImpact CurrentImpact { get; private set; }

        public void SetClimate(ClimateProfile profile) => activeClimate = profile;

        public void RerollWeather(float timeOfDay)
        {
            if (activeClimate == null) return;

            if (!initialized || UnityEngine.Random.value > activeClimate.persistenceFactor)
            {
                CurrentWeather = RollNewWeather();
                CurrentImpact = WeatherImpact.Get(CurrentWeather);
                initialized = true;
            }

            UpdateTemperature(timeOfDay);
        }

        private WeatherType RollNewWeather()
        {
            float total = activeClimate.GetTotalWeight();
            if (total <= 0) return WeatherType.Sunny;

            float roll = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;

            foreach (WeatherType type in Enum.GetValues(typeof(WeatherType)))
            {
                cumulative += activeClimate.GetWeight(type);
                if (roll < cumulative) return type;
            }

            return WeatherType.Sunny;
        }

        private void UpdateTemperature(float timeOfDay)
        {
            float baseTemp = (activeClimate.minTemp + activeClimate.maxTemp) * 0.5f;
            float amplitude = (activeClimate.maxTemp - activeClimate.minTemp) * 0.5f;
            float diurnalOffset = -Mathf.Cos(((timeOfDay - 2f) / 24f) * Mathf.PI * 2f) * amplitude;
            float weatherOffset = UnityEngine.Random.Range(CurrentImpact.temperatureMin, CurrentImpact.temperatureMax);
            
            float jitter = UnityEngine.Random.Range(-activeClimate.temperatureVariability, 
                                          activeClimate.temperatureVariability) * activeClimate.jitterStrength;

            CurrentTemperature = baseTemp + diurnalOffset + weatherOffset + jitter;
        }
    }
}
