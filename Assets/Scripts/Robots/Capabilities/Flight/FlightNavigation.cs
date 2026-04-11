using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Robots.Models;

namespace Robots.Capabilities.Flight
{
    public class FlightNavigation
    {
        private List<EnvironmentalSensor> trackedParcels = new List<EnvironmentalSensor>();
        private OperationRegion region;
        private Transform droneTransform;
        private CropDatabase cropDB;

        public EnvironmentalSensor CurrentTarget { get; private set; }
        public float LastUrgency { get; private set; }
        public float LastDistance { get; private set; }
        public OperationRegion Region => region;

        public void SetupRegion(Transform robotTransform)
        {
            droneTransform = robotTransform;
            var fence = Object.FindFirstObjectByType<FenceGenerator>();
            if (fence?.zones != null && fence.zones.Length > 0)
            {
                var nearestZone = FindNearestZone(robotTransform.position, fence.zones);
                region = OperationRegion.FromZone(nearestZone);
            }
            else
                region = new OperationRegion(new Rect(0, 0, 1000, 1000));

            cropDB = CropLoader.Load();
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
                if (sqrDist < minSqrDist) { minSqrDist = sqrDist; best = zone; }
            }
            return best;
        }

        public void RefreshParcels()
        {
            if (ParcelCache.Instance == null) return;
            trackedParcels = region.FilterParcels(ParcelCache.Parcels);
            if (trackedParcels.Count == 0) trackedParcels.AddRange(ParcelCache.Parcels);
        }

        public EnvironmentalSensor SelectNextTarget()
        {
            if (trackedParcels.Count == 0 || droneTransform == null) return null;

            EnvironmentalSensor best = null;
            float bestPriority = -1f;

            foreach (var parcel in trackedParcels)
            {
                if (parcel == null || !NeedsTreatment(parcel)) continue;

                float urgency = CalculateUrgency(parcel);
                float dist = Vector3.Distance(droneTransform.position, parcel.transform.position);
                float priority = urgency / Mathf.Max(dist, 1f);

                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    best = parcel;
                    LastUrgency = urgency;
                    LastDistance = dist;
                }
            }

            CurrentTarget = best;
            return best;
        }

        private float CalculateUrgency(EnvironmentalSensor parcel)
        {
            var data = cropDB?.Get(parcel.plantedVarietyName);
            float optN = data?.requirements?.nitrogen?.optimal ?? 100f;
            float deficit = Mathf.Max(0, optN - parcel.nitrogen);
            return (deficit / Mathf.Max(optN, 1f)) * 100f;
        }

        public bool NeedsTreatment(EnvironmentalSensor parcel)
        {
            if (parcel == null) return false;
            var data = cropDB?.Get(parcel.plantedVarietyName);
            float requiredN = data?.requirements?.nitrogen?.optimal ?? 100f;
            return parcel.nitrogen < requiredN * 0.95f;
        }

        public bool HasTargets => trackedParcels.Count > 0;

        public List<(EnvironmentalSensor parcel, float urgency, float dist)> GetTopAlternatives(int count)
        {
            var results = new List<(EnvironmentalSensor, float, float)>();
            if (droneTransform == null) return results;

            foreach (var parcel in trackedParcels)
            {
                if (parcel == null || parcel == CurrentTarget || !NeedsTreatment(parcel)) continue;
                float urgency = CalculateUrgency(parcel);
                float dist = Vector3.Distance(droneTransform.position, parcel.transform.position);
                results.Add((parcel, urgency, dist));
            }

            results.Sort((a, b) => (b.Item2 / Mathf.Max(b.Item3, 1f)).CompareTo(a.Item2 / Mathf.Max(a.Item3, 1f)));
            if (results.Count > count) results.RemoveRange(count, results.Count - count);
            return results;
        }
    }
}
