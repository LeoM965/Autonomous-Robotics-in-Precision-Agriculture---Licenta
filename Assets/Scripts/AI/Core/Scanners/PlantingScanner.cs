using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

namespace AI.Core.Scanners
{
    [CreateAssetMenu(fileName = "PlantingScanner", menuName = "AI/Scanners/Planting Scanner")]
    public class PlantingScanner : BaseScanner
    {
        [SerializeField] private float minSoilQuality = 30f;
        
        public override void Scan(List<RobotTask> tasks, FenceZone[] zones)
        {
            if (ParcelCache.Instance == null) return;

            var db = CropLoader.Load();
            float avgYieldValue = AverageCropStat(db, c => c.yieldValueEUR, 1f);
            float avgSeedCost   = AverageCropStat(db, c => c.seedCostEUR,   5f);

            foreach (var parcel in ParcelCache.Instance.ParcelsIterator)
            {
                if (parcel == null || parcel.isScheduledForTask || 
                    parcel.activeCrops.Count > 0 || parcel.soilQuality < minSoilQuality)
                    continue;

                int zoneIdx = GetOrCreateZoneIndex(parcel, zones);
                if (zoneIdx >= 0)
                {
                    // Pre-check if any crop is suitable right now (weather/season permitting)
                    var bestCrop = CropSelector.SelectBestCrop(db, parcel, null, 1, 0f, false);
                    if (bestCrop != null)
                    {
                        float suitability = parcel.soilQuality / 100f;
                        float gain = suitability * avgYieldValue;
                        float cost = avgSeedCost;
                        tasks.Add(new PlantingTask(parcel.transform, gain, cost));
                        parcel.isScheduledForTask = true;
                    }
                }
            }
        }

        private static float AverageCropStat(CropDatabase db, System.Func<CropData, float> selector, float fallback)
        {
            if (db?.crops == null || db.crops.Length == 0) return fallback;
            float total = 0f;
            foreach (var crop in db.crops) total += selector(crop);
            return total / db.crops.Length;
        }
    }
}

