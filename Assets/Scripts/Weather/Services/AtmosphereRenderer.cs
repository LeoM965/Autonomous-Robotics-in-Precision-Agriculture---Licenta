using UnityEngine;
using Weather.Models;

namespace Weather.Services
{
    public class AtmosphereRenderer
    {
        private Light _sunLight;
        private float _lerpSpeed = 1.0f;

        public AtmosphereRenderer(Light sun)
        {
            _sunLight = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        public void Render(WeatherProfile profile, float deltaTime)
        {
            if (profile == null) return;

            // 1. Skybox Transition
            if (RenderSettings.skybox != profile.skyboxMaterial)
            {
                RenderSettings.skybox = profile.skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }

            // 2. Light Transitions
            if (_sunLight != null)
            {
                _sunLight.color = Color.Lerp(_sunLight.color, profile.sunColor, deltaTime * _lerpSpeed);
                _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, profile.sunIntensity, deltaTime * _lerpSpeed);
            }

            // 3. Fog Transitions
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, profile.fogColor, deltaTime * _lerpSpeed);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, profile.fogDensity, deltaTime * _lerpSpeed);
            
            // 4. Ambient Transition
            RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, profile.ambientIntensity, deltaTime * _lerpSpeed);
        }
    }
}
