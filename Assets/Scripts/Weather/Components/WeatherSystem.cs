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

        [Header("References")]
        public Light directionalLight;
        public PrecipitationManager precipitation;

        private WeatherSimulator simulator;
        private AtmosphereRenderer renderer;

        public WeatherType? ForcedWeather => simulator?.ForcedWeather;
        public WeatherType CurrentWeather => simulator?.CurrentWeather ?? WeatherType.Sunny;
        public float CurrentTemperature => simulator?.CurrentTemperature ?? 20f;
        public WeatherImpact CurrentImpact => simulator?.CurrentImpact ?? new WeatherImpact();
        public ClimateProfile ActiveClimate { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            simulator = new WeatherSimulator();
            renderer = new AtmosphereRenderer(directionalLight);
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += () => UpdateClimate();
                TimeManager.Instance.OnHourChanged += (hour) => simulator.RerollWeather(hour);
            }

            UpdateClimate();
            float startTime = TimeManager.Instance != null ? TimeManager.Instance.timeOfDay : 12f;
            simulator.RerollWeather(startTime);
        }

        // Soil moisture e procesat DOAR de WeatherSoilUpdater (acelasi GameObject)
        private void Update()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (simulator == null || weatherProfiles == null) return;
            WeatherProfile profile = GetProfile(simulator.CurrentWeather);
            if (profile == null) return;

            float time = TimeManager.Instance != null ? TimeManager.Instance.timeOfDay : 12f;
            renderer.Render(profile, time, Time.deltaTime);

            if (precipitation != null)
                precipitation.UpdateEffects(simulator.CurrentWeather, 1.0f);
        }

        private void UpdateClimate()
        {
            if (TimeManager.Instance == null) return;
            Season s = TimeManager.Instance.GetCurrentSeason();
            if (climates == null) return;
            ClimateProfile profile = climates.Find(c => c.seasonType == s);
            if (profile != null && simulator != null)
            {
                simulator.SetClimate(profile);
                ActiveClimate = profile;
            }
        }

        public float GetMovementPenalty()
        {
            float seasonalBase = 1.0f;
            if (ActiveClimate != null)
            {
                seasonalBase = ActiveClimate.movementMultiplier;
            }
            if (simulator == null) return seasonalBase;
            return seasonalBase * simulator.CurrentImpact.movementSpeed;
        }

        public float GetCropGrowthMultiplier() => simulator?.CurrentImpact.cropGrowth ?? 1f;

        public void SetForcedWeather(WeatherType? type)
        {
            if (simulator == null) return;
            simulator.ForcedWeather = type;
            if (TimeManager.Instance != null)
                simulator.RerollWeather(TimeManager.Instance.timeOfDay);
        }

        public void CycleForcedWeather()
        {
            if (simulator == null) return;
            int currentIndex = -1;
            if (simulator.ForcedWeather.HasValue)
                currentIndex = System.Array.IndexOf(WeatherTypes.All, simulator.ForcedWeather.Value);

            currentIndex++;
            if (currentIndex >= WeatherTypes.All.Length)
                SetForcedWeather(null);
            else
                SetForcedWeather(WeatherTypes.All[currentIndex]);
        }

        private WeatherProfile GetProfile(WeatherType type) => weatherProfiles?.Find(p => p.type == type);
    }
}
