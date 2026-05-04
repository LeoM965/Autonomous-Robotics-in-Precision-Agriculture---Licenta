using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Economics.Models;
using Economics.Services;
using Weather.Models;

namespace Economics.Managers
{
    public class EconomicsHistoryManager : MonoBehaviour
    {
        public static EconomicsHistoryManager Instance;

        [SerializeField] private List<DailySnapshot> history = new List<DailySnapshot>();
        public List<DailySnapshot> History => history;

        /// <summary>Restaurează istoricul zilnic din save.</summary>
        public void RestoreHistory(List<DailySnapshot> saved)
        {
            history.Clear();
            if (saved != null) history.AddRange(saved);
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += OnDayChanged;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= OnDayChanged;
            }
        }

        private void OnDayChanged()
        {
            if (TimeManager.Instance != null)
                CaptureSnapshot(TimeManager.Instance.currentDay - 1);
        }

        public void CaptureSnapshot(int dayIndex)
        {
            CropDatabase db = CropLoader.Load();
            EconomicReport report = CropEconomicsCalculator.GetAnalysis(db);

            DailySnapshot snapshot = new DailySnapshot
            {
                Day = dayIndex,
                SeasonName = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentSeason().ToString() : "N/A",
                TotalRevenue = report.FarmTotals.TotalRevenue,
                TotalCosts = report.FarmTotals.TotalSeedCost + report.FarmTotals.TotalOperationalCost,
                NetProfit = report.FarmTotals.NetProfit,
                TotalWeightKg = report.FarmTotals.TotalWeightKg,
                TotalPlants = report.FarmTotals.TotalPlants + report.FarmTotals.HarvestedPlants
            };

            // Calculate deltas based on previous day
            if (history.Count > 0)
            {
                var prev = history[history.Count - 1];
                snapshot.ProfitDelta = snapshot.NetProfit - prev.NetProfit;
                snapshot.RevenueDelta = snapshot.TotalRevenue - prev.TotalRevenue;
            }
            else
            {
                snapshot.ProfitDelta = snapshot.NetProfit;
                snapshot.RevenueDelta = snapshot.TotalRevenue;
            }

            history.Add(snapshot);
            Debug.Log($"[EconomicsHistoryManager] Snapshot captured for Day {dayIndex} ({snapshot.SeasonName}). Profit: {snapshot.NetProfit:F2}€");
        }

        [ContextMenu("Export History to CSV")]
        public void ExportToCSV()
        {
            if (history.Count == 0)
            {
                Debug.LogWarning("[EconomicsHistoryManager] Nu exista date pentru export!");
                return;
            }

            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Day,Season,TotalRevenue,TotalCosts,NetProfit,ProfitDelta,RevenueDelta,WeightKg,TotalPlants");

            foreach (var s in history)
            {
                csv.AppendLine(Inv($"{s.Day},{s.SeasonName},{s.TotalRevenue:F2},{s.TotalCosts:F2},{s.NetProfit:F2},{s.ProfitDelta:F2},{s.RevenueDelta:F2},{s.TotalWeightKg:F2},{s.TotalPlants}"));
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"SimulationResults_{timestamp}.csv";
            string path = Path.Combine(Application.dataPath, "..", fileName);

            try
            {
                File.WriteAllText(path, csv.ToString());
                Debug.Log($"<color=green><b>[EconomicsHistoryManager]</b> Export reusit la: {path}</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EconomicsHistoryManager] Eroare la export: {e.Message}");
            }
        }

        public void ExportCropsToCSV()
        {
            CropDatabase db = CropLoader.Load();
            if (db?.crops == null || db.crops.Length == 0)
            {
                Debug.LogWarning("[EconomicsHistoryManager] Nu exista culturi pentru export!");
                return;
            }

            EconomicReport report = CropEconomicsCalculator.GetAnalysis(db);
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Variety,Plants,Harvested,SeedCost,Revenue,WeightKg,Profit,ROI%,SoilFit%");

            foreach (var crop in db.crops)
            {
                if (!report.AnalysisByVariety.TryGetValue(crop.name, out var stats)) continue;
                float profit = stats.TotalRevenue - stats.TotalSeedCost;
                float roi = stats.TotalSeedCost > 0 ? (profit / stats.TotalSeedCost) * 100f : 0f;
                csv.AppendLine(Inv($"{crop.name},{stats.TotalPlants},{stats.HarvestedPlants},{stats.TotalSeedCost:F2},{stats.TotalRevenue:F2},{stats.TotalWeightKg:F2},{profit:F2},{roi:F1},{stats.AvgSoilCompatibility:F1}"));
            }

            var totals = report.FarmTotals;
            float totalProfit = totals.NetProfit;
            float totalROI = (totals.TotalSeedCost + totals.TotalOperationalCost) > 0
                ? (totalProfit / (totals.TotalSeedCost + totals.TotalOperationalCost)) * 100f : 0f;
            csv.AppendLine(Inv($"TOTAL,{totals.TotalPlants},{totals.HarvestedPlants},{totals.TotalSeedCost:F2},{totals.TotalRevenue:F2},{totals.TotalWeightKg:F2},{totalProfit:F2},{totalROI:F1},"));

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"CropResults_{timestamp}.csv";
            string path = Path.Combine(Application.dataPath, "..", fileName);

            try
            {
                File.WriteAllText(path, csv.ToString());
                Debug.Log($"<color=green><b>[EconomicsHistoryManager]</b> Export culturi reusit la: {path}</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EconomicsHistoryManager] Eroare la export culturi: {e.Message}");
            }
        }

        /// <summary>
        /// Formateaza cu punct decimal (InvariantCulture), nu virgula — esential pentru CSV valid.
        /// </summary>
        private static string Inv(FormattableString fs)
            => fs.ToString(CultureInfo.InvariantCulture);
    }
}
