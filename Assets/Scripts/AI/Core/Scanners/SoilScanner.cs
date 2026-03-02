using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Sensors.Models;
using Sensors.Services;

namespace AI.Core.Scanners
{
    public class SoilScanner : ITaskScanner
    {
        private readonly FenceZone[] zones;

        public SoilScanner(FenceZone[] zones)
        {
            this.zones = zones;
        }

        public void Scan(List<RobotTask> tasks)
        {
            if (ParcelCache.Instance == null) return;

            foreach (var parcel in ParcelCache.Instance.ParcelsIterator)
            {
                if (parcel == null || parcel.composition == null || parcel.isScheduledForTask)
                    continue;

                SoilAnalysis analysis = parcel.LatestAnalysis;
                if (analysis.HasAlerts)
                {
                    int zoneIdx = GetOrCreateZoneIndex(parcel);
                    if (zoneIdx >= 0)
                    {
                        TaskType type = GetTaskType(analysis);
                        tasks.Add(new RobotTask(parcel.transform, type, analysis.qualityScore));
                        parcel.isScheduledForTask = true;
                    }
                }
            }
        }

        private int GetOrCreateZoneIndex(EnvironmentalSensor parcel)
        {
            if (parcel.zoneIndex != -1) return parcel.zoneIndex;

            FenceZone zone = BoundsHelper.FindZoneContaining(parcel.transform.position, zones);
            if (zone != null)
            {
                parcel.zoneIndex = System.Array.IndexOf(zones, zone);
            }
            return parcel.zoneIndex;
        }

        private TaskType GetTaskType(SoilAnalysis analysis)
        {
            if (analysis.requiresIrrigation) return TaskType.Irrigate;
            if (analysis.requiresFertilization) return TaskType.Fertilize;
            if (analysis.requiresLiming) return TaskType.Lime;
            return TaskType.Scout;
        }
    }
}
