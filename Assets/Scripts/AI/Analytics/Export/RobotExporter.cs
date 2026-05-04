using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Economics.Managers;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Exporta starea completa a fiecarui robot (pozitie, energie, costuri, venituri).
    /// </summary>
    public class RobotExporter : ICsvExporter
    {
        private const string Header =
            "sim_run,day,robot_id,name,type,model,zone,posX,posZ," +
            "energy_kwh,distance_m,speed,purchase_price,maint_cost," +
            "depr_cost,total_cost,revenue,roi,is_idle";

        public void Export(ExportContext ctx)
        {
            if (RobotEconomicsManager.Instance == null) return;

            var rows = new List<string>();
            foreach (var entry in RobotEconomicsManager.Instance.RobotStatsMap)
            {
                Transform t = entry.Key;
                if (t == null) continue;

                var s = entry.Value;
                rows.Add(Csv.Line(
                    $"{ctx.RunId},{ctx.Day},{t.GetInstanceID()},{t.name},{s.type},",
                    $"{s.model ?? ""},{s.zone},{t.position.x:F2},{t.position.z:F2},",
                    $"{s.energykWh:F3},{s.distance:F1},{s.speed:F2},{s.purchasePrice:F0},",
                    $"{s.maintenanceCost:F2},{s.depreciationCost:F2},{s.TotalCost:F2},",
                    $"{s.revenueGenerated:F2},{s.ROI:F2},{(s.IsIdle ? 1 : 0)}"
                ));
            }

            Csv.Write(Path.Combine(ctx.FolderPath, $"robots_day_{ctx.Day}.csv"), Header, rows);
        }
    }
}
