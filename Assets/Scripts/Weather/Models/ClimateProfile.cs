using UnityEngine;

namespace Weather.Models
{
    [CreateAssetMenu(fileName = "New Climate Profile", menuName = "Robotics/Weather/Climate Profile")]
    public class ClimateProfile : ScriptableObject
    {
        [Header("Identity")]
        public string seasonName;
        public Season seasonType;

        [Header("Temperature Range")]
        public float minTemp = 10f;
        public float maxTemp = 25f;
        public float temperatureVariability = 2.0f;
        public float jitterStrength = 0.3f;

        [Header("Probabilities")]
        public float persistenceFactor = 0.7f;
        [Range(0, 1)] public float sunnyWeight = 0.6f;
        [Range(0, 1)] public float rainyWeight = 0.2f;
        [Range(0, 1)] public float stormyWeight = 0.05f;
        [Range(0, 1)] public float snowyWeight = 0f;
        [Range(0, 1)] public float foggyWeight = 0.15f;

        [Header("Impact Scales")]
        public float movementMultiplier = 1.0f;
        public float evaporationRate = 5.0f;

        public float GetTotalWeight() => sunnyWeight + rainyWeight + stormyWeight + snowyWeight + foggyWeight;
        
        public float GetWeight(WeatherType type) => type switch
        {
            WeatherType.Sunny => sunnyWeight,
            WeatherType.Rainy => rainyWeight,
            WeatherType.Stormy => stormyWeight,
            WeatherType.Snowy => snowyWeight,
            WeatherType.Foggy => foggyWeight,
            _ => 0f
        };
    }

    public enum Season { Spring, Summer, Autumn, Winter }
}
