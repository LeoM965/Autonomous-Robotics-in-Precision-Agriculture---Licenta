using System.Collections.Generic;
using System.IO;
using Economics.Managers;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Exporta evolutia zilnica a profitului (istoric cumulativ).
    /// </summary>
    public class HistoryExporter : ICsvExporter
    {
        private const string Header =
            "sim_run,day,season,total_revenue,total_costs,net_profit," +
            "profit_delta,revenue_delta,weight_kg,total_plants";

        public void Export(ExportContext ctx)
        {
            if (EconomicsHistoryManager.Instance == null) return;

            var hist = EconomicsHistoryManager.Instance.History;
            if (hist.Count == 0) return;

            var rows = new List<string>(hist.Count);
            foreach (var s in hist)
            {
                rows.Add(Csv.Line(
                    $"{ctx.RunId},{s.Day},{s.SeasonName},",
                    $"{s.TotalRevenue:F2},{s.TotalCosts:F2},{s.NetProfit:F2},",
                    $"{s.ProfitDelta:F2},{s.RevenueDelta:F2},",
                    $"{s.TotalWeightKg:F2},{s.TotalPlants}"
                ));
            }

            Csv.Write(Path.Combine(ctx.FolderPath, "history.csv"), Header, rows);
        }
    }
}
