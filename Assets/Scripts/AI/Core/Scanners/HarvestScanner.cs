using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

namespace AI.Core.Scanners
{
    public class HarvestScanner : ITaskScanner
    {
        private readonly FenceZone[] zones;

        public HarvestScanner(FenceZone[] zones)
        {
            this.zones = zones;
        }

        public void Scan(List<RobotTask> tasks)
        {
            if (ParcelCache.Instance == null) return;

            foreach (var parcel in ParcelCache.Instance.ParcelsIterator)
            {
                if (parcel == null || parcel.isScheduledForTask || parcel.activeCrops.Count == 0) 
                    continue;

                int matureCount = GetMatureCropCount(parcel);
                if (matureCount == 0) continue;

                int zoneIdx = GetOrCreateZoneIndex(parcel);
                if (zoneIdx >= 0)
                {
                    float priority = matureCount * 10f;
                    tasks.Add(new RobotTask(parcel.transform, TaskType.Harvest, priority));
                    parcel.isScheduledForTask = true;
                }
            }
        }

        private int GetMatureCropCount(EnvironmentalSensor parcel)
        {
            int count = 0;
            foreach (var crop in parcel.activeCrops)
            {
                if (crop != null && crop.IsFullyGrown) count++;
            }
            return count;
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
    }
}
