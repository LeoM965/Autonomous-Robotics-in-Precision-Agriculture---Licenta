using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Robots.Models;

namespace Robots.Capabilities.Flight
{
    public class FlightNavigation
    {
        private List<EnvironmentalSensor> trackedParcels = new List<EnvironmentalSensor>();
        private int currentParcelIndex = -1;
        private OperationRegion region;

        public EnvironmentalSensor CurrentTarget { get; private set; }

        public void Initialize(OperationRegion region)
        {
            this.region = region;
            PopulateParcels();
        }

        public void PopulateParcels()
        {
            if (ParcelCache.Instance == null) return;
            trackedParcels = region.FilterParcels(ParcelCache.Parcels);
            if (trackedParcels.Count == 0) trackedParcels.AddRange(ParcelCache.Parcels);
            
            // Prioritize by health/quality
            trackedParcels.Sort((a, b) => a.soilQuality.CompareTo(b.soilQuality));
        }

        public EnvironmentalSensor SelectNextTarget()
        {
            if (trackedParcels == null || trackedParcels.Count == 0) return null;

            // Search for the first parcel that needs treatment according to thresholds
            // We start from the next index to ensure we don't get stuck on one
            int startIdx = (currentParcelIndex + 1) % trackedParcels.Count;
            
            for (int i = 0; i < trackedParcels.Count; i++)
            {
                int checkIdx = (startIdx + i) % trackedParcels.Count;
                var p = trackedParcels[checkIdx];

                bool needsNutrients = p.nitrogen < 100f; // Fallback
                var data = CropLoader.Load()?.Get(p.plantedVarietyName);
                if (data?.requirements?.nitrogen != null)
                {
                    needsNutrients = p.nitrogen < data.requirements.nitrogen.optimal;
                }

                if (needsNutrients)
                {
                    currentParcelIndex = checkIdx;
                    CurrentTarget = p;
                    return CurrentTarget;
                }
            }

            // If no parcel needs urgent care, we can either hover or visit the one with lowest quality
            currentParcelIndex = (currentParcelIndex + 1) % trackedParcels.Count;
            CurrentTarget = trackedParcels[currentParcelIndex];
            return CurrentTarget;
        }

        public bool HasTargets => trackedParcels.Count > 0;
    }
}
