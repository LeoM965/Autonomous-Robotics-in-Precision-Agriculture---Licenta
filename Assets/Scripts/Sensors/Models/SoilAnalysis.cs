using UnityEngine;

namespace Sensors.Models
{
    public enum SoilHealthStatus
    {
        Excellent,
        Optimal,
        Suboptimal,
        Deficient,
        Critical
    }

    [System.Serializable]
    public struct SoilAnalysis
    {
        public float qualityScore; // 0-100
        public SoilHealthStatus health;
        
        [Header("Intervention Flags")]
        public bool requiresIrrigation;
        public bool requiresFertilization;
        public bool requiresLiming;
        public bool requiresAcidification;

        [Header("Deficits")]
        public float nitrogenDeficit;
        public float phosphorusDeficit;
        public float potassiumDeficit;
        public float moistureDeficit;

        public bool HasAlerts => requiresIrrigation || requiresFertilization || requiresLiming || requiresAcidification;
    }
}
