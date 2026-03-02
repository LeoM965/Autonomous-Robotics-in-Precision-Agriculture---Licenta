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
        private float lastSimHours = -1f;

        public WeatherType CurrentWeather => simulator.CurrentWeather;
        public float CurrentTemperature => simulator.CurrentTemperature;
        public WeatherImpact CurrentImpact => simulator.CurrentImpact;
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
                TimeManager.Instance.OnDayChanged += (_) => UpdateClimate();
                TimeManager.Instance.OnHourChanged += (hour) => simulator.RerollWeather(hour);
            }

            UpdateClimate();
            float startTime = TimeManager.Instance != null ? TimeManager.Instance.timeOfDay : 12f;
            simulator.RerollWeather(startTime);
        }

        private void Update()
        {
            UpdateVisuals();
            ProcessSoilMoisture();
        }

        private void UpdateVisuals()
        {
            WeatherProfile profile = GetProfile(simulator.CurrentWeather);
            if (profile == null) return;

            renderer.Render(profile, Time.deltaTime);

            if (precipitation != null)
                precipitation.UpdateEffects(simulator.CurrentWeather, 1.0f);
        }

        private void UpdateClimate()
        {
            if (TimeManager.Instance == null) return;
            Season s = TimeManager.Instance.GetCurrentSeason();
            ClimateProfile profile = climates.Find(c => c.seasonType == s);
            if (profile != null)
            {
                simulator.SetClimate(profile);
                ActiveClimate = profile;
            }
        }

        private void ProcessSoilMoisture()
        {
            if (TimeManager.Instance == null || ActiveClimate == null) return;

            float currentSimHours = (TimeManager.Instance.currentDay - 1) * 24f + TimeManager.Instance.timeOfDay;
            if (lastSimHours < 0f) { lastSimHours = currentSimHours; return; }

            float deltaHours = currentSimHours - lastSimHours;
            if (deltaHours <= 0f) return;

            lastSimHours = currentSimHours;
            SoilMoistureService.UpdateMoisture(ParcelCache.Parcels, simulator.CurrentImpact, ActiveClimate, deltaHours);
        }

        public float GetMovementPenalty()
        {
            float seasonalBase = ActiveClimate != null ? ActiveClimate.movementMultiplier : 1.0f;
            return seasonalBase * simulator.CurrentImpact.movementSpeed;
        }

        public float GetCropGrowthMultiplier() => simulator.CurrentImpact.cropGrowth;

        private WeatherProfile GetProfile(WeatherType type) => weatherProfiles.Find(p => p.type == type);
    }
}
