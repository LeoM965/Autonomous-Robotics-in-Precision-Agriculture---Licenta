using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sensors.Components;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Exporta compozitia solului, cultura plantata si recolta per parcela.
    /// </summary>
    public class ParcelExporter : ICsvExporter
    {
        private const string Header =
            "sim_run,day,parcel_id,name,zone,posX,posZ,soil_type,pH,moisture," +
            "nitrogen,phosphorus,potassium,quality,health_status,has_alerts," +
            "needs_irrigation,needs_fertilization,needs_liming,needs_acidification," +
            "n_deficit,p_deficit,k_deficit,moisture_deficit," +
            "season,temperature,weather," +
            "variety,growth_pct,stage,harvest_count,harvest_kg,harvest_revenue," +
            "seed_cost,net_profit";

        public void Export(ExportContext ctx)
        {
            if (!ParcelCache.HasInstance) return;

            var rows = new List<string>();
            foreach (var p in ParcelCache.Instance.ParcelsIterator)
            {
                if (p == null) continue;
                rows.Add(FormatRow(ctx, p));
            }

            Csv.Write(Path.Combine(ctx.FolderPath, $"parcels_day_{ctx.Day}.csv"), Header, rows);
        }

        private string FormatRow(ExportContext ctx, EnvironmentalSensor p)
        {
            Vector3 pos = p.transform.position;
            string zone = p.name.Contains("_") ? p.name.Split('_')[1].Substring(0, 1) : "?";
            string variety = string.IsNullOrEmpty(p.plantedVarietyName) ? "" : p.plantedVarietyName;
            float netProfit = p.harvestedRevenue - p.harvestedSeedCost;
            var analysis = p.LatestAnalysis;

            return Csv.Line(
                $"{ctx.RunId},{ctx.Day},{p.GetInstanceID()},{p.name},{zone},",
                $"{pos.x:F2},{pos.z:F2},{p.detectedType},{p.soilPH:F2},{p.soilMoisture:F1},",
                $"{p.nitrogen:F1},{p.phosphorus:F1},{p.potassium:F1},{p.soilQuality:F1},",
                $"{analysis.health},{(analysis.HasAlerts ? 1 : 0)},",
                $"{(analysis.requiresIrrigation ? 1 : 0)},{(analysis.requiresFertilization ? 1 : 0)},",
                $"{(analysis.requiresLiming ? 1 : 0)},{(analysis.requiresAcidification ? 1 : 0)},",
                $"{analysis.nitrogenDeficit:F1},{analysis.phosphorusDeficit:F1},",
                $"{analysis.potassiumDeficit:F1},{analysis.moistureDeficit:F1},",
                $"{ctx.Season},{ctx.Temperature:F1},{ctx.WeatherCondition},",
                $"{variety},{p.growthProgress:F1},{p.currentGrowthStage},",
                $"{p.harvestedCount},{p.harvestedWeightKg:F2},{p.harvestedRevenue:F2},",
                $"{p.harvestedSeedCost:F2},{netProfit:F2}"
            );
        }
    }
}
