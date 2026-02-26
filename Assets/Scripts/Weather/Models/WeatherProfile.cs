using UnityEngine;

namespace Weather.Models
{
    [CreateAssetMenu(fileName = "New Weather Profile", menuName = "Robotics/Weather/Profile")]
    public class WeatherProfile : ScriptableObject
    {
        public WeatherType type;
        
        [Header("Atmosphere")]
        public Material skyboxMaterial;
        public Color fogColor = Color.gray;
        public float fogDensity = 0.01f;
        
        [Header("Lighting")]
        public Color sunColor = Color.white;
        public float sunIntensity = 1.0f;
        public float ambientIntensity = 1.0f;

        [Header("Precipitation")]
        public bool hasPrecipitation;
        public float precipitationIntensity = 1.0f;
    }
}
