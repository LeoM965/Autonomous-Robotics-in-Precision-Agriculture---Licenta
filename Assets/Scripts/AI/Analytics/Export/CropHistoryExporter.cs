using System.Collections.Generic;
using System.IO;
using Sensors.Components;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Exporta istoricul cumulativ per varietate de cultura (sezoane anterioare).
    /// Datele provin din EnvironmentalSensor.CropHistory.
    /// </summary>
    public class CropHistoryExporter : ICsvExporter
    {
        private const string Header =
            "sim_run,day,variety,total_plants,total_revenue,total_weight_kg," +
            "total_seed_cost,profit";

        public void Export(ExportContext ctx)
        {
            var history = EnvironmentalSensor.CropHistory;
            if (history == null || history.Count == 0) return;

            var rows = new List<string>();
            foreach (var kvp in history)
            {
                var r = kvp.Value;
                rows.Add(Csv.Line(
                    $"{ctx.RunId},{ctx.Day},{kvp.Key},",
                    $"{r.totalPlants},{r.totalRevenue:F2},{r.totalWeightKg:F2},",
                    $"{r.totalSeedCost:F2},{r.Profit:F2}"
                ));
            }

            Csv.Write(Path.Combine(ctx.FolderPath, $"crop_history_day_{ctx.Day}.csv"), Header, rows);
        }
    }
}
