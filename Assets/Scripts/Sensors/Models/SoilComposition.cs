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
        [Tooltip("Nitrogen in kg/ha")]
        public float nitrogen;
        [Tooltip("Phosphorus in kg/ha")]
        public float phosphorus;
        [Tooltip("Potassium in kg/ha")]
        public float potassium;

        [Header("Dynamics")]
        public float irrigationRate;
    }
}
