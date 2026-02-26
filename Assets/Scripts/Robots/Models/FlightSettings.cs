using UnityEngine;

namespace Robots.Models
{
    [System.Serializable]
    public class FlightSettings
    {
        public float altitude = 8f;
        public float speed = 6f;
        public float arrivalThreshold = 2f;
        public float waitTimePerParcel = 3f;
        public float hoverAmplitude = 0.15f;
        public float hoverFrequency = 1.2f;

        public float ArrivalThresholdSqr => arrivalThreshold * arrivalThreshold;
        public float AngularHoverFrequency => hoverFrequency * Mathf.PI * 2f;
    }
}
