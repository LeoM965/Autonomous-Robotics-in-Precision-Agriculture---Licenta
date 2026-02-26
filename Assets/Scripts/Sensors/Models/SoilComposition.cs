using UnityEngine;

namespace Sensors.Models
{
    [System.Serializable]
    public class SoilComposition
    {
        [Header("Hydrology")]
        [Range(0, 100)] public float moisture; // Replaced 'humidity' for better terminology
        
        [Header("Chemical Properties")]
        public float pH;
        
        [Header("Nutrients (NPK)")]
        [Tooltip("Nitrogen in ppm")]
        public float nitrogen;
        [Tooltip("Phosphorus in ppm")]
        public float phosphorus;
        [Tooltip("Potassium in ppm")]
        public float potassium;

        [Header("Dynamics")]
        public float irrigationRate;
    }
}
