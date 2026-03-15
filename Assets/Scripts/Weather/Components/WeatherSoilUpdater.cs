using UnityEngine;
using Sensors.Services;
using Weather.Services;

namespace Weather.Components
{
    [RequireComponent(typeof(WeatherSystem))]
    public class WeatherSoilUpdater : MonoBehaviour
    {
        private WeatherSystem weatherSystem;
        private float lastSimHours = -1f;
        private float updateTimer;
        private const float UPDATE_INTERVAL = 1.0f;

        private void Awake()
        {
            weatherSystem = GetComponent<WeatherSystem>();
        }

        private void Update()
        {
            if (weatherSystem == null || weatherSystem.ActiveClimate == null || TimeManager.Instance == null) return;

            updateTimer += Time.deltaTime;
            if (updateTimer < UPDATE_INTERVAL) return;
            updateTimer = 0f;

            float currentSimHours = TimeManager.Instance.TotalSimulatedHours;

            if (!TimeManager.Instance.IsInitialized)
            {
                lastSimHours = currentSimHours;
                return;
            }

            if (lastSimHours < 0f) 
            { 
                lastSimHours = currentSimHours; 
                return; 
            }

            float deltaHours = currentSimHours - lastSimHours;

            if (deltaHours > 1.0f || deltaHours < 0f) 
            {
                lastSimHours = currentSimHours;
                return;
            }

            lastSimHours = currentSimHours;
            
            if (ParcelCache.HasInstance)
            {
                SoilMoistureService.UpdateMoisture(
                    ParcelCache.Parcels, 
                    weatherSystem.CurrentImpact, 
                    weatherSystem.ActiveClimate, 
                    deltaHours
                );
            }
        }
    }
}
