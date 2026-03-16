using UnityEngine;

namespace Sensors.Models
{
    [System.Serializable]
    public class SoilTypeRanges
    {
        public AgroSoilType type;
        [Header("Ranges (min, max)")]
        public Vector2 moisture;
        public Vector2 pH;
        [Tooltip("Nitrogen in kg/ha")]
        public Vector2 nitrogen;
        [Tooltip("Phosphorus in kg/ha")]
        public Vector2 phosphorus;
        [Tooltip("Potassium in kg/ha")]
        public Vector2 potassium;

        public SoilComposition Generate()
        {
            return new SoilComposition
            {
                moisture = Random.Range(moisture.x, moisture.y),
                pH = Random.Range(pH.x, pH.y),
                nitrogen = Random.Range(nitrogen.x, nitrogen.y),
                phosphorus = Random.Range(phosphorus.x, phosphorus.y),
                potassium = Random.Range(potassium.x, potassium.y)
            };
        }
    }

    [CreateAssetMenu(fileName = "SoilSettings", menuName = "Robotics/Sensors/Soil Settings")]
    public class SoilSettings : ScriptableObject
    {
        [Header("Nutrient Optimal Values (kg/ha)")]
        public float nOptimal = 130f;
        public float pOptimal = 80f;
        public float kOptimal = 120f;

        [Header("Nutrient Critical Thresholds (kg/ha)")]
        public float nCritical = 70f;
        public float pCritical = 40f;
        public float kCritical = 60f;

        [Header("pH Thresholds")]
        public float phIdeal = 6.5f;
        public float phMin = 5.5f;
        public float phMax = 7.8f;

        [Header("Moisture Thresholds (%)")]
        public float moistureMin = 35f;
        public float moistureOptimal = 55f;

        [Header("Quality Score Weights (must sum to 100)")]
        public float phWeight = 30f;
        public float moistureWeight = 25f;
        public float nitrogenWeight = 20f;
        public float phosphorusWeight = 15f;
        public float potassiumWeight = 10f;

        [Header("Quality Score Tolerances")]
        public float phTolerance = 1.5f;
        public float moistureTolerance = 25f;

        [Header("Health Score Thresholds")]
        public float excellentThreshold = 85f;
        public float optimalThreshold = 65f;
        public float suboptimalThreshold = 45f;
        public float deficientThreshold = 25f;

        [Header("Soil Generation Ranges")]
        public SoilTypeRanges[] typeRanges;
    }
}
