using UnityEngine;
using System.Collections.Generic;
using Economics.Models;
using Economics.Services;

namespace Economics.Managers
{
    public class EconomicsHistoryManager : MonoBehaviour
    {
        public static EconomicsHistoryManager Instance;

        [SerializeField] private List<DailySnapshot> history = new List<DailySnapshot>();
        public List<DailySnapshot> History => history;

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
            Debug.Log($"[EconomicsHistoryManager] Snapshot captured for Day {dayIndex}. Profit: {snapshot.NetProfit:F2}€");
        }
    }
}
