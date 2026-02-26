using UnityEngine;
using Weather.Models;
using Weather.Services;
using System.Collections.Generic;

namespace Weather.Components
{
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [Header("Configurations")]
        public List<ClimateProfile> climates;
        public List<WeatherProfile> weatherProfiles;

        [Header("Standard References")]
        public Light directionalLight;
        public PrecipitationManager precipitation;

        private WeatherSimulator _simulator;
        private AtmosphereRenderer _renderer;
        
        public WeatherType CurrentWeather => _simulator.CurrentWeather;
        public float CurrentTemperature => _simulator.CurrentTemperature;
        public ClimateProfile ActiveClimate { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            _simulator = new WeatherSimulator();
            _renderer = new AtmosphereRenderer(directionalLight);
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += (day) => UpdateClimate();
                TimeManager.Instance.OnHourChanged += (hour) => _simulator.RerollWeather(hour);
            }

            UpdateClimate();
            _simulator.RerollWeather(TimeManager.Instance != null ? TimeManager.Instance.timeOfDay : 12f, true);
        }

        private void Update()
        {
            float time = TimeManager.Instance != null ? TimeManager.Instance.timeOfDay : 12f;

            WeatherProfile activeProfile = GetProfile(_simulator.CurrentWeather);
            _renderer.Render(activeProfile, Time.deltaTime);
            
            if (precipitation != null)
                precipitation.UpdateEffects(_simulator.CurrentWeather, 1.0f);
        }

        private void UpdateClimate()
        {
            if (TimeManager.Instance == null) return;
            
            Season s = TimeManager.Instance.GetCurrentSeason();
            ClimateProfile profile = climates.Find(c => c.seasonType == s);
            
            if (profile != null) 
            {
                _simulator.SetClimate(profile);
                ActiveClimate = profile;
            }
        }

        public float GetMovementPenalty()
        {
            float penalty = ActiveClimate != null ? ActiveClimate.movementSpeedMultiplier : 1.0f;
            
            if (CurrentWeather == WeatherType.Stormy) penalty *= 0.5f;
            if (CurrentWeather == WeatherType.Snowy) penalty *= 0.6f;
            if (CurrentWeather == WeatherType.Foggy) penalty *= 0.9f;
                
            return penalty;
        }

        private WeatherProfile GetProfile(WeatherType type)
        {
            return weatherProfiles.Find(p => p.type == type);
        }
    }
}
