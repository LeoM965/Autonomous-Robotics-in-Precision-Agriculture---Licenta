using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Economics.Models;
using Economics.Managers;

namespace Economics.Services
{
    public static class CropEconomicsCalculator
    {
        private static EconomicReport lastReport;
        private static int lastCalculatedFrame = -1;

        public static EconomicReport GetAnalysis(CropDatabase db)
        {
            if (Time.frameCount == lastCalculatedFrame) return lastReport;

            var report = new EconomicReport 
            { 
                AnalysisByVariety = new Dictionary<string, CropStats>() 
            };

            if (db?.crops == null) return report;

            float[] yieldWeights = Settings.SimulationSettings.YieldWeights;
            float[] marketPrices = Settings.SimulationSettings.MarketPrices;

            foreach (var parcel in ParcelCache.Parcels)
            {
                if (!IsParcelPlanted(parcel)) continue;
                
                string varietyName = parcel.plantedVarietyName;
                int cropIndex = db.GetIndex(varietyName);
                if (cropIndex < 0) continue;

                var stats = GetOrCreateVarietyStats(report.AnalysisByVariety, varietyName);
                ProcessParcelData(ref stats, db.crops[cropIndex], parcel, yieldWeights, marketPrices, cropIndex);
                report.AnalysisByVariety[varietyName] = stats;
            }

            CalculateFarmTotals(ref report);
            lastReport = report;
            lastCalculatedFrame = Time.frameCount;
            return report;
        }

        private static bool IsParcelPlanted(EnvironmentalSensor p) 
            => p != null && p.activeCrops != null 
               && (!string.IsNullOrEmpty(p.plantedVarietyName) || p.harvestedCount > 0);

        private static CropStats GetOrCreateVarietyStats(Dictionary<string, CropStats> dict, string name) 
            => dict.TryGetValue(name, out var existing) ? existing : new CropStats { VarietyName = name };

        private static void ProcessParcelData(ref CropStats stats, CropData crop, EnvironmentalSensor parcel, float[] yWeights, float[] mPrices, int idx)
        {
            stats.TotalPlants += parcel.activeCrops.Count;
            stats.HarvestedPlants += parcel.harvestedCount;
            stats.ParcelCount++;
            
            if (crop.requirements != null)
                stats.SoilFitSum += crop.requirements.CalculateTotalScore(parcel.composition);

            // Add accumulated harvested stats (persist after plants destroyed)
            stats.HarvestedRevenue += parcel.harvestedRevenue;
            stats.TotalWeightKg += parcel.harvestedWeightKg;
            stats.TotalSeedCost += parcel.harvestedSeedCost;

            // Add stats from plants still in the field
            float soilQualityMultiplier = parcel.LatestAnalysis.qualityScore / 100f;
            float baseWeight = (yWeights != null && idx < yWeights.Length) ? yWeights[idx] : crop.yieldWeightKg;
            float marketPrice = (mPrices != null && idx < mPrices.Length) ? mPrices[idx] : crop.marketPricePerKg;

            foreach (var plant in parcel.activeCrops)
            {
                if (plant == null) continue;
                float actualWeight = baseWeight * soilQualityMultiplier * plant.Progress;
                stats.AddPlantData(actualWeight, marketPrice, plant.PurchasePrice);
            }
        }

        private static void CalculateFarmTotals(ref EconomicReport report)
        {
            var summary = new CropStats { VarietyName = "TOTAL" };
            
            foreach (var varietyStats in report.AnalysisByVariety.Values)
            {
                summary.TotalPlants += varietyStats.TotalPlants;
                summary.HarvestedPlants += varietyStats.HarvestedPlants;
                summary.FieldRevenue += varietyStats.FieldRevenue;
                summary.HarvestedRevenue += varietyStats.HarvestedRevenue;
                summary.TotalWeightKg += varietyStats.TotalWeightKg;
                summary.TotalSeedCost += varietyStats.TotalSeedCost;
                summary.SoilFitSum += varietyStats.SoilFitSum;
                summary.ParcelCount += varietyStats.ParcelCount;
            }

            if (RobotEconomicsManager.Instance != null)
            {
                summary.TotalEnergyCost = RobotEconomicsManager.Instance.GlobalEnergyCost;
                summary.TotalMaintenanceCost = RobotEconomicsManager.Instance.GlobalMaintenanceCost;
                summary.TotalDepreciationCost = RobotEconomicsManager.Instance.GlobalDepreciationCost;
            }
            
            report.FarmTotals = summary;
        }
    }
}
