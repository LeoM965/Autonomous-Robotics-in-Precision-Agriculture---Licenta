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

            UpdateSystem(rainSystem, type == WeatherType.Rainy || type == WeatherType.Stormy, targetIntensity);
            UpdateSystem(snowSystem, type == WeatherType.Snowy, targetIntensity);
            UpdateSystem(fogSystem, type == WeatherType.Foggy, targetIntensity);
        }

        private void UpdateSystem(GameObject system, bool active, float intensity)
        {
            if (system == null) return;
            system.SetActive(active);
            if (active) SetIntensity(system, intensity);
        }

        private void SetIntensity(GameObject system, float value)
        {
            if (system.TryGetComponent<RainAreaController>(out var controller))
            {
                controller.SetIntensity(value);
            }
            else if (system.TryGetComponent<ParticleSystem>(out var ps) || (ps = system.GetComponentInChildren<ParticleSystem>()) != null)
            {
                var emission = ps.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(500f * value);
            }
        }
    }
}
