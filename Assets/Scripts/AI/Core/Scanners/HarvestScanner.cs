using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

namespace AI.Core.Scanners
{
    [CreateAssetMenu(fileName = "HarvestScanner", menuName = "AI/Scanners/Harvest Scanner")]
    public class HarvestScanner : BaseScanner
    {
        public override void Scan(List<RobotTask> tasks, FenceZone[] zones)
        {
            if (ParcelCache.Instance == null) return;

            foreach (var parcel in ParcelCache.Instance.ParcelsIterator)
            {
                if (parcel == null || parcel.isScheduledForTask || parcel.activeCrops.Count == 0) 
                    continue;

                int matureCount = GetMatureCropCount(parcel);
                if (matureCount == 0) continue;

                int zoneIdx = GetOrCreateZoneIndex(parcel, zones);
                if (zoneIdx >= 0)
                {
                    float gain = matureCount * 25f; // Economic gain from harvesting
                    tasks.Add(new HarvestTask(parcel.transform, gain));
                    parcel.isScheduledForTask = true;
                }
            }
        }

        private int GetMatureCropCount(EnvironmentalSensor parcel)
        {
            int count = 0;
            foreach (var crop in parcel.activeCrops)
            {
                if (crop != null && crop.IsHarvestable) count++;
            }
            return count;
        }
    }
}
