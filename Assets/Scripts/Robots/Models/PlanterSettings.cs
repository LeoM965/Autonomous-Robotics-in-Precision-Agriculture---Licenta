using UnityEngine;

namespace Robots.Models
{
    [System.Serializable]
    public class PlanterSettings
    {
        public float arriveDistance = 2.5f;
        public float scanInterval = 10f;
        public float minSoilQuality = 0.3f;
        public float plantDelay = 0.5f;
    }
}
