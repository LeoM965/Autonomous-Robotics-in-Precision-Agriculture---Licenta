using UnityEngine;

namespace Robots.Models
{
    [System.Serializable]
    public class HarvesterSettings
    {
        public float arriveDistance = 4f;
        public float rescanInterval = 10f;
        public float minSoilQuality = 0.1f;
        public float harvestRadius = 2.5f;
        public float harvestDelay = 1.0f;
    }
}
