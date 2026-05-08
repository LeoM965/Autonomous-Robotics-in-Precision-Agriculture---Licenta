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

        /// <summary>
        /// Calculates combined NPK urgency (0–100) as a weighted average of each nutrient's deficit.
        /// Weights: N=50%, P=30%, K=20% — reflecting agronomic importance.
        /// </summary>
        private float CalculateUrgency(EnvironmentalSensor parcel)
        {
            var data = cropDB?.Get(parcel.plantedVarietyName);
            float optN = data?.requirements?.nitrogen?.optimal ?? 100f;
            float optP = data?.requirements?.phosphorus?.optimal ?? 50f;
            float optK = data?.requirements?.potassium?.optimal ?? 50f;
            float optPH = data?.requirements?.pH?.optimal ?? 6.5f;

            float defN = Mathf.Max(0, optN - parcel.nitrogen) / Mathf.Max(optN, 1f);
            float defP = Mathf.Max(0, optP - parcel.phosphorus) / Mathf.Max(optP, 1f);
            float defK = Mathf.Max(0, optK - parcel.potassium) / Mathf.Max(optK, 1f);
            float defPH = Mathf.Min(Mathf.Abs(optPH - parcel.soilPH) / 1.5f, 1f);

            // Weighted average: N=40%, P=20%, K=10%, PH=30%
            return (defN * 0.4f + defP * 0.2f + defK * 0.1f + defPH * 0.3f) * 100f;
        }

        /// <summary>
        /// A parcel needs treatment if ANY of N, P, K is below 80% of its optimal value.
        /// </summary>
        public bool NeedsTreatment(EnvironmentalSensor parcel)
        {
            if (parcel == null) return false;
            if (string.IsNullOrEmpty(parcel.plantedVarietyName)) return false; // no crop = no treatment needed

            var data = cropDB?.Get(parcel.plantedVarietyName);

            float optN = data?.requirements?.nitrogen?.optimal ?? 100f;
            float optP = data?.requirements?.phosphorus?.optimal ?? 50f;
            float optK = data?.requirements?.potassium?.optimal ?? 50f;
            float optPH = data?.requirements?.pH?.optimal ?? 6.5f;

            return parcel.nitrogen < optN * 0.80f
                || parcel.phosphorus < optP * 0.80f
                || parcel.potassium < optK * 0.80f
                || Mathf.Abs(parcel.soilPH - optPH) > 0.4f;
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
