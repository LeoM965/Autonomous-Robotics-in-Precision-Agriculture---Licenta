using UnityEngine;
using System.IO;
using System.Text;
using Sensors.Components;
using Economics.Managers;
using Economics.Models;
using Economics.Services;
using AI.Analytics;

namespace AI.Analytics
{
    public class DataExporter : MonoBehaviour
    {
        private string folderPath;

        private void Awake()
        {
            folderPath = Path.Combine(Application.dataPath, "..", "Exported_SimData");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged += SaveDailySnapshot;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= SaveDailySnapshot;
        }

        [ContextMenu("Export Snapshot")]
        public void SaveDailySnapshot()
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.currentDay - 1 : 0;
            
            ExportRobots(day);
            ExportParcels(day);
            ExportDecisions(day);
            ExportEconomy(day);
            ExportHistory();
            
            Debug.Log($"<color=cyan><b>[DataExporter]</b> 5 CSV-uri exportate pentru Ziua {day}.</color>");
        }

        private void ExportRobots(int day)
        {
            if (RobotEconomicsManager.Instance == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("robot_id,name,type,zone,posX,posZ,energy_kwh,distance_m,speed,maint_cost,depr_cost,total_cost,revenue,roi");

            foreach (var entry in RobotEconomicsManager.Instance.RobotStatsMap)
            {
                Transform t = entry.Key;
                var s = entry.Value;
                if (t == null) continue;
                sb.AppendLine($"{t.GetInstanceID()},{t.name},{s.type},{s.zone},{t.position.x:F2},{t.position.z:F2},{s.energykWh:F3},{s.distance:F1},{s.speed:F2},{s.maintenanceCost:F2},{s.depreciationCost:F2},{s.TotalCost:F2},{s.revenueGenerated:F2},{s.ROI:F2}");
            }
            File.WriteAllText(Path.Combine(folderPath, $"robots_day_{day}.csv"), sb.ToString());
        }

        private void ExportParcels(int day)
        {
            if (ParcelCache.Instance == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("parcel_id,name,posX,posZ,moisture,nitrogen,phosphorus,potassium,quality,variety,growth_pct,harvest_count,harvest_kg,harvest_revenue,seed_cost");

            foreach (var p in ParcelCache.Instance.ParcelsIterator)
            {
                if (p == null) continue;
                Vector3 pos = p.transform.position;
                sb.AppendLine($"{p.GetInstanceID()},{p.name},{pos.x:F2},{pos.z:F2},{p.soilMoisture:F1},{p.nitrogen:F1},{p.phosphorus:F1},{p.potassium:F1},{p.soilQuality:F1},{p.plantedVarietyName},{p.growthProgress:F1},{p.harvestedCount},{p.harvestedWeightKg:F2},{p.harvestedRevenue:F2},{p.harvestedSeedCost:F2}");
            }
            File.WriteAllText(Path.Combine(folderPath, $"parcels_day_{day}.csv"), sb.ToString());
        }

        private void ExportDecisions(int day)
        {
            if (DecisionTracker.Instance == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("global_index,robot_name,type,option,parcel,score,net_value,timestamp");

            foreach (var entry in RobotEconomicsManager.Instance.RobotStatsMap)
            {
                if (entry.Key == null) continue;
                var history = DecisionTracker.Instance.GetRecentDecisions(entry.Key, 200);
                foreach (var d in history)
                    sb.AppendLine($"{d.globalIndex},{entry.Key.name},{d.decisionType},{d.chosenOption},{d.parcelName},{d.chosenScore:F2},{d.netValue:F2},{d.timestamp:F2}");
            }
            File.WriteAllText(Path.Combine(folderPath, $"decisions_day_{day}.csv"), sb.ToString());
        }

        private void ExportEconomy(int day)
        {
            CropDatabase db = CropLoader.Load();
            if (db?.crops == null) return;
            EconomicReport report = CropEconomicsCalculator.GetAnalysis(db);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("variety,total_plants,harvested,seed_cost,revenue,weight_kg,profit,roi_pct,soil_fit_pct");

            foreach (var crop in db.crops)
            {
                if (!report.AnalysisByVariety.TryGetValue(crop.name, out var s)) continue;
                float profit = s.TotalRevenue - s.TotalSeedCost;
                float roi = s.TotalSeedCost > 0 ? (profit / s.TotalSeedCost) * 100f : 0f;
                sb.AppendLine($"{crop.name},{s.TotalPlants},{s.HarvestedPlants},{s.TotalSeedCost:F2},{s.TotalRevenue:F2},{s.TotalWeightKg:F2},{profit:F2},{roi:F1},{s.AvgSoilCompatibility:F1}");
            }
            File.WriteAllText(Path.Combine(folderPath, $"economy_day_{day}.csv"), sb.ToString());
        }

        private void ExportHistory()
        {
            if (EconomicsHistoryManager.Instance == null) return;
            var hist = EconomicsHistoryManager.Instance.History;
            if (hist.Count == 0) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("day,season,total_revenue,total_costs,net_profit,profit_delta,revenue_delta,weight_kg,total_plants");

            foreach (var s in hist)
                sb.AppendLine($"{s.Day},{s.SeasonName},{s.TotalRevenue:F2},{s.TotalCosts:F2},{s.NetProfit:F2},{s.ProfitDelta:F2},{s.RevenueDelta:F2},{s.TotalWeightKg:F2},{s.TotalPlants}");

            File.WriteAllText(Path.Combine(folderPath, "history.csv"), sb.ToString());
        }
    }
}
