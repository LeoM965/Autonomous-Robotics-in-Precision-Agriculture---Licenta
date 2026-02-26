using UnityEngine;
using System;

namespace Weather.Models
{
    [CreateAssetMenu(fileName = "New Climate Profile", menuName = "Robotics/Weather/Climate Profile")]
    public class ClimateProfile : ScriptableObject
    {
        [Header("Identity")]
        public string seasonName;
        public Season seasonType;

        [Header("Temperature Range (°C)")]
        public float minTemp = 10f;
        public float maxTemp = 25f;

        [Header("Probability Weights (0 to 1)")]
        [Range(0, 1)] public float sunnyChance = 0.6f;
        [Range(0, 1)] public float rainyChance = 0.2f;
        [Range(0, 1)] public float stormyChance = 0.05f;
        [Range(0, 1)] public float snowyChance = 0f;
        [Range(0, 1)] public float foggyChance = 0.15f;

        [Header("Environmental Impact")]
        public float temperatureVariability = 2.0f;
        public float movementSpeedMultiplier = 1.0f;
        public float evaporationRate = 5.0f;
        public float precipitationIntensityScale = 1.0f;
    }

    public enum Season { Spring, Summer, Autumn, Winter }
}
