using UnityEngine;
using Weather.Models;
using System.Collections.Generic;

namespace Weather.Services
{
    public class WeatherSimulator
    {
        private ClimateProfile _activeClimate;
        
        public WeatherType CurrentWeather { get; private set; }
        public float CurrentTemperature { get; private set; }

        public void SetClimate(ClimateProfile profile)
        {
            _activeClimate = profile;
        }

        public void RerollWeather(float timeOfDay, bool force = false)
        {
            if (_activeClimate == null) return;

            // Simple weighted random selection
            float total = _activeClimate.sunnyChance + _activeClimate.rainyChance + 
                          _activeClimate.stormyChance + _activeClimate.snowyChance + 
                          _activeClimate.foggyChance;
            
            float roll = Random.Range(0, total);
            
            if (roll < _activeClimate.sunnyChance) CurrentWeather = WeatherType.Sunny;
            else if (roll < _activeClimate.sunnyChance + _activeClimate.rainyChance) CurrentWeather = WeatherType.Rainy;
            else if (roll < _activeClimate.sunnyChance + _activeClimate.rainyChance + _activeClimate.stormyChance) CurrentWeather = WeatherType.Stormy;
            else if (roll < _activeClimate.sunnyChance + _activeClimate.rainyChance + _activeClimate.stormyChance + _activeClimate.snowyChance) CurrentWeather = WeatherType.Snowy;
            else CurrentWeather = WeatherType.Foggy;

            UpdateTemperature(timeOfDay);
        }

        public void UpdateTemperature(float timeOfDay)
        {
            if (_activeClimate == null) return;
            
            float baseTemp = (_activeClimate.minTemp + _activeClimate.maxTemp) / 2f;
            // Peak at ~14:00, coolest at ~04:00
            float timeFactor = -Mathf.Cos(((timeOfDay - 2f) / 24f) * Mathf.PI * 2) * ((_activeClimate.maxTemp - _activeClimate.minTemp) / 2f);
            
            // Random jitter based on climate variability
            float jitter = Random.Range(-_activeClimate.temperatureVariability, _activeClimate.temperatureVariability);

            // Weather impact offset (randomized)
            float weatherOffset = 0f;
            if (CurrentWeather == WeatherType.Rainy || CurrentWeather == WeatherType.Stormy) 
                weatherOffset = Random.Range(-4.52f, -1.88f);
            if (CurrentWeather == WeatherType.Snowy) 
                weatherOffset = Random.Range(-10.25f, -6.15f);
            
            CurrentTemperature = baseTemp + timeFactor + weatherOffset + jitter;
        }
    }
}
