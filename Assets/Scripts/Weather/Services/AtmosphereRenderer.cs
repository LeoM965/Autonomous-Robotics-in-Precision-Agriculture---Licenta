using UnityEngine;
using Weather.Models;

namespace Weather.Services
{
    public class AtmosphereRenderer
    {
        private Light sunLight;
        private float lerpSpeed = 1.0f;

        public AtmosphereRenderer(Light sun)
        {
            sunLight = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        public void Render(WeatherProfile profile, float deltaTime)
        {
            if (profile == null) return;

            if (RenderSettings.skybox != profile.skyboxMaterial)
            {
                RenderSettings.skybox = profile.skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }

            if (sunLight != null)
            {
                sunLight.color = Color.Lerp(sunLight.color, profile.sunColor, deltaTime * lerpSpeed);
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, profile.sunIntensity, deltaTime * lerpSpeed);
            }

            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, profile.fogColor, deltaTime * lerpSpeed);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, profile.fogDensity, deltaTime * lerpSpeed);
            RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, profile.ambientIntensity, deltaTime * lerpSpeed);
        }
    }
}
