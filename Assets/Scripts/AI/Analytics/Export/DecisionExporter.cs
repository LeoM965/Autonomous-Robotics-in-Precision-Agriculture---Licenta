using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Economics.Managers;
using AI.Analytics;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Exporta deciziile AI noi (delta fata de ultimul export, nu duplicat).
    /// </summary>
    public class DecisionExporter : ICsvExporter
    {
        private const string Header =
            "sim_run,day,decision_id,robot_name,decision_type,chosen_option," +
            "parcel,score,net_value,ph_score,nitrogen_score,phosphorus_score," +
            "potassium_score,humidity_score,scheduling_value,num_alternatives,timestamp,ml_prediction";

        private readonly Dictionary<Transform, int> lastExportedCount = new Dictionary<Transform, int>();

        public void Export(ExportContext ctx)
        {
            if (DecisionTracker.Instance == null || RobotEconomicsManager.Instance == null) return;

            var rows = new List<string>();
            foreach (var entry in RobotEconomicsManager.Instance.RobotStatsMap)
            {
                if (entry.Key == null) continue;
                ExportNewDecisions(ctx, entry.Key, rows);
            }

            if (rows.Count > 0)
                Csv.Write(Path.Combine(ctx.FolderPath, $"decisions_day_{ctx.Day}.csv"), Header, rows);
        }

        private void ExportNewDecisions(ExportContext ctx, Transform robot, List<string> rows)
        {
            int totalCount = DecisionTracker.Instance.GetTotalDecisions(robot);

            if (!lastExportedCount.TryGetValue(robot, out int lastCount))
                lastCount = 0;

            if (totalCount <= lastCount) return;

            int newCount = totalCount - lastCount;
            var recent = DecisionTracker.Instance.GetRecentDecisions(robot, newCount);

            foreach (var d in recent)
            {
                int altCount = d.alternatives != null ? d.alternatives.Count : 0;

                rows.Add(Csv.Line(
                    $"{ctx.RunId},{ctx.Day},{d.globalIndex},{robot.name},",
                    $"{d.decisionType},{d.chosenOption},{d.parcelName},",
                    $"{d.chosenScore:F2},{d.netValue:F2},",
                    $"{d.factors.phScore:F1},{d.factors.nitrogenScore:F1},",
                    $"{d.factors.phosphorusScore:F1},{d.factors.potassiumScore:F1},",
                    $"{d.factors.humidityScore:F1},{d.schedulingValue:F2},",
                    $"{altCount},{d.timestamp:F2},{d.mlPrediction ?? ""}"
                ));
            }

            lastExportedCount[robot] = totalCount;
        }
    }
}
