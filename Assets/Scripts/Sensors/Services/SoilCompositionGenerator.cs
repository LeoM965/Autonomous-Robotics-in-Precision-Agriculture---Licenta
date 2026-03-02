using UnityEngine;
using Sensors.Models;

namespace Sensors.Services
{
    public static class SoilCompositionGenerator
    {
        public static SoilComposition Generate(AgroSoilType type, SoilSettings settings)
        {
            if (settings != null && settings.typeRanges != null)
            {
                foreach (var range in settings.typeRanges)
                {
                    if (range.type == type) return range.Generate();
                }
            }

            // Simple safe fallback if asset is missing or type not found. 
            // Better to have one minimal fallback here than duplication.
            return new SoilComposition
            {
                moisture = 45f,
                pH = 6.5f,
                nitrogen = 80f,
                phosphorus = 20f,
                potassium = 180f
            };
        }
    }
}
