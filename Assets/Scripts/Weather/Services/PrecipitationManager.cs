using UnityEngine;
using Weather.Models;

namespace Weather.Services
{
    public class PrecipitationManager : MonoBehaviour
    {
        [Header("FX References")]
        public GameObject rainSystem;
        public GameObject snowSystem;
        public GameObject fogSystem;

        public void UpdateEffects(WeatherType type, float intensity)
        {
            float multiplier = (type == WeatherType.Stormy) ? 2.0f : 1.0f;
            float targetIntensity = intensity * multiplier;

            if (rainSystem != null) 
            {
                bool active = type == WeatherType.Rainy || type == WeatherType.Stormy;
                rainSystem.SetActive(active);
                if (active) SetIntensity(rainSystem, targetIntensity);
            }
            
            if (snowSystem != null)
            {
                bool active = type == WeatherType.Snowy;
                snowSystem.SetActive(active);
                if (active) SetIntensity(snowSystem, targetIntensity);
            }

            if (fogSystem != null)
            {
                bool active = type == WeatherType.Foggy;
                fogSystem.SetActive(active);
                if (active) SetIntensity(fogSystem, targetIntensity);
            }
        }

        private void SetIntensity(GameObject system, float value)
        {
            // Try RainAreaController (existing in project)
            var controller = system.GetComponent<RainAreaController>();
            if (controller != null)
            {
                controller.SetIntensity(value);
                return;
            }

            // Fallback: Try raw ParticleSystem
            var ps = system.GetComponent<ParticleSystem>();
            if (ps == null) ps = system.GetComponentInChildren<ParticleSystem>();
            
            if (ps != null)
            {
                var emission = ps.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(500f * value);
            }
        }
    }
}
