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

        public void SetupRegion(Transform robotTransform)
        {
            var fence = Object.FindFirstObjectByType<FenceGenerator>();
            if (fence?.zones != null && fence.zones.Length > 0)
            {
                var nearestZone = FindNearestZone(robotTransform.position, fence.zones);
                region = OperationRegion.FromZone(nearestZone);
            }
            else
            {
                region = new OperationRegion(new Rect(0, 0, 1000, 1000));
            }
            RefreshParcels();
        }

        private FenceZone FindNearestZone(Vector3 pos, FenceZone[] zones)
        {
            float minSqrDist = float.MaxValue;
            FenceZone best = zones[0];
            foreach (var zone in zones)
            {
                Vector2 center = (zone.startXZ + zone.endXZ) * 0.5f;
                float sqrDist = (pos.x - center.x) * (pos.x - center.x) + (pos.z - center.y) * (pos.z - center.y);
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    best = zone;
                }
            }
            return best;
        }

        public void RefreshParcels()
        {
            if (ParcelCache.Instance == null) return;
            trackedParcels = region.FilterParcels(ParcelCache.Parcels);
            if (trackedParcels.Count == 0) trackedParcels.AddRange(ParcelCache.Parcels);
            trackedParcels.Sort((a, b) => a.soilQuality.CompareTo(b.soilQuality));
        }

        public EnvironmentalSensor SelectNextTarget()
        {
            if (trackedParcels.Count == 0) return null;
            currentParcelIndex = (currentParcelIndex + 1) % trackedParcels.Count;
            CurrentTarget = trackedParcels[currentParcelIndex];
            return CurrentTarget;
        }

        public EnvironmentalSensor PeekNextTarget()
        {
            if (trackedParcels.Count == 0) return null;
            int nextIdx = (currentParcelIndex + 1) % trackedParcels.Count;
            return trackedParcels[nextIdx];
        }

        public bool HasTargets => trackedParcels.Count > 0;
    }
}
