using UnityEngine;
using Weather.Components;
using Weather.Models;

public class SoilHydrologyManager : MonoBehaviour
{
    private float _lastSimHours = -1f;
    private float _updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.5f;

    private void Update()
    {
        if (TimeManager.Instance == null || WeatherSystem.Instance == null) return;

        _updateTimer += Time.deltaTime;
        if (_updateTimer < UPDATE_INTERVAL) return;
        
        float currentSimHours = (TimeManager.Instance.currentDay - 1) * 24f + TimeManager.Instance.timeOfDay;
        
        if (_lastSimHours < 0)
        {
            _lastSimHours = currentSimHours;
            _updateTimer = 0f;
            return;
        }

        float delta = currentSimHours - _lastSimHours;
        if (delta > 0)
        {
            ProcessAcademicWaterBalance(delta);
            _lastSimHours = currentSimHours;
        }
        _updateTimer = 0f;
    }

    private void ProcessAcademicWaterBalance(float dt)
    {
        WeatherSystem ws = WeatherSystem.Instance;
        ClimateProfile config = ws.ActiveClimate;

        float tod = TimeManager.Instance.timeOfDay;
        bool isSun = tod > 6f && tod < 19f;

        float rain = 5.0f; 
        float storm = 15.0f;
        float fog = 0.05f;
        float infLimit = 12f;
        float drainStd = 0.1f;
        float drainSat = 1.2f;
        float threshold = 80f;
        float evRate = 4.0f;

        if (config != null) evRate = config.evaporationRate;

        float precip = 0f;
        if (ws.CurrentWeather == WeatherType.Rainy) precip = rain;
        else if (ws.CurrentWeather == WeatherType.Stormy) precip = storm;
        else if (ws.CurrentWeather == WeatherType.Foggy) precip = fog;

        float et = (evRate / 24f) * (isSun ? 1.6f : 0.4f);
        et *= Mathf.Clamp(ws.CurrentTemperature / 20f, 0.5f, 2.5f);
        if (ws.CurrentWeather == WeatherType.Foggy || ws.CurrentWeather == WeatherType.Cloudy) et *= 0.2f;

        foreach (var p in ParcelCache.Parcels)
        {
            if (p == null || p.composition == null) continue;

            float netMm = (Mathf.Min(precip + p.composition.irrigationRate, infLimit) - et) * dt;
            float h = p.composition.moisture;
            
            float t = Mathf.InverseLerp(threshold - 10f, threshold + 10f, h);
            float drainage = Mathf.Lerp(drainStd, drainSat, t);

            float lastMoisture = p.composition.moisture;
            p.composition.moisture = Mathf.Clamp(h + netMm - (drainage * dt), 0, 100);
            p.composition.irrigationRate = 0f;
            
            // Only refresh visuals if moisture changed by at least 1%
            if (Mathf.Abs(lastMoisture - p.composition.moisture) > 1.0f)
            {
                p.Analyze();
            }
        }
    }
}
