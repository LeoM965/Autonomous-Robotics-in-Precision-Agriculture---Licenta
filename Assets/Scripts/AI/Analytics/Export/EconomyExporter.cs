using System.Collections.Generic;
using System.IO;
using Economics.Services;
using Economics.Models;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Exporta analiza economica per varietate de cultura (costuri, venituri, ROI).
    /// </summary>
    public class EconomyExporter : ICsvExporter
    {
        private const string Header =
            "sim_run,day,variety,frost_resistant,seed_cost_eur,market_price_eur_kg," +
            "yield_kg,growth_days,total_plants,harvested,total_seed_cost,revenue," +
            "weight_kg,profit,roi_pct,soil_fit_pct,energy_cost,maint_cost,depr_cost";

        public void Export(ExportContext ctx)
        {
            CropDatabase db = CropLoader.Load();
            if (db?.crops == null) return;

            EconomicReport report = CropEconomicsCalculator.GetAnalysis(db);
            var rows = new List<string>();

            foreach (var crop in db.crops)
            {
                if (!report.AnalysisByVariety.TryGetValue(crop.name, out var s)) continue;
                rows.Add(FormatRow(ctx, crop, s));
            }

            if (rows.Count > 0)
                Csv.Write(Path.Combine(ctx.FolderPath, $"economy_day_{ctx.Day}.csv"), Header, rows);
        }

        private string FormatRow(ExportContext ctx, CropData crop, CropStats s)
        {
            float totalCost = s.TotalSeedCost + s.TotalOperationalCost;
            float profit = s.TotalRevenue - totalCost;
            float roi = totalCost > 0 ? (profit / totalCost) * 100f : 0f;

            return Csv.Line(
                $"{ctx.RunId},{ctx.Day},{crop.name},{(crop.isFrostResistant ? 1 : 0)},",
                $"{crop.seedCostEUR:F3},{crop.marketPricePerKg:F2},{crop.yieldWeightKg:F3},",
                $"{crop.growthDays},{s.TotalPlants},{s.HarvestedPlants},",
                $"{s.TotalSeedCost:F2},{s.TotalRevenue:F2},{s.TotalWeightKg:F2},",
                $"{profit:F2},{roi:F1},{s.AvgSoilCompatibility:F1},",
                $"{s.TotalEnergyCost:F2},{s.TotalMaintenanceCost:F2},{s.TotalDepreciationCost:F2}"
            );
        }
    }
}
