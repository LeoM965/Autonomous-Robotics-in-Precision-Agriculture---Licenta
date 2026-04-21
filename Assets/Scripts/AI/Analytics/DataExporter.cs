using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
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
        private int simulationRunId;
        private string runFolder;

        // Tracking pentru a exporta doar deciziile NOI (nu duplicat)
        private readonly System.Collections.Generic.Dictionary<Transform, int> lastExportedDecisionCount 
            = new System.Collections.Generic.Dictionary<Transform, int>();

        // Tracking pentru vreme orara (acumuleaza pe parcursul zilei)
        private readonly System.Collections.Generic.List<WeatherHourEntry> weatherLog 
            = new System.Collections.Generic.List<WeatherHourEntry>();

        private struct WeatherHourEntry
        {
            public int day;
            public float hour;
            public string season;
            public string weather;
            public float temperature;
            public float cropGrowthMult;
            public float movementPenalty;
        }

        private const string RUN_COUNTER_FILE = "sim_run_counter.txt";

        private void Awake()
        {
            string basePath = Path.Combine(Application.dataPath, "..", "Exported_SimData");
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

            // Incrementam numarul de simulare la fiecare rulare
            simulationRunId = LoadAndIncrementRunCounter(basePath);
            runFolder = Path.Combine(basePath, $"Run_{simulationRunId:D3}");
            if (!Directory.Exists(runFolder)) Directory.CreateDirectory(runFolder);

            folderPath = runFolder;
            // Nu mai exportam metadata aici pentru ca entitatile (parcele/roboti) nu sunt inca initializate in Awake
        }

        private int LoadAndIncrementRunCounter(string basePath)
        {
            string counterPath = Path.Combine(basePath, RUN_COUNTER_FILE);
            int runId = 1;

            if (File.Exists(counterPath))
            {
                if (int.TryParse(File.ReadAllText(counterPath).Trim(), out int existing))
                    runId = existing + 1;
            }

            File.WriteAllText(counterPath, runId.ToString());
            return runId;
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += SaveDailySnapshot;
                TimeManager.Instance.OnHourChanged += LogWeatherHour;
            }
            
            // Exportam metadata cu un mic delay sau pur si simplu in Start (dupa Awake-urile altora)
            Invoke(nameof(ExportMetadata), 0.5f);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= SaveDailySnapshot;
                TimeManager.Instance.OnHourChanged -= LogWeatherHour;
            }
            // Salvam ce a ramas in weather log la inchidere (zi partiala)
            if (weatherLog.Count > 0)
                ExportWeather(TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0);
        }

        private void LogWeatherHour(float hour)
        {
            var ws = Weather.Components.WeatherSystem.Instance;
            int day = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0;

            weatherLog.Add(new WeatherHourEntry
            {
                day = day,
                hour = hour,
                season = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentSeason().ToString() : "",
                weather = ws != null ? ws.CurrentWeather.ToString() : "",
                temperature = ws != null ? ws.CurrentTemperature : 0f,
                cropGrowthMult = ws != null ? ws.GetCropGrowthMultiplier() : 1f,
                movementPenalty = ws != null ? ws.GetMovementPenalty() : 1f
            });
        }

        // ──────────────────────────────────────────────
        // Metadata — informatii despre rularea curenta
        // ──────────────────────────────────────────────
        private void ExportMetadata()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"simulation_run,{simulationRunId}");
            sb.AppendLine($"start_time,{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"unity_version,{Application.unityVersion}");
            sb.AppendLine($"platform,{Application.platform}");
            sb.AppendLine($"parcels,{(ParcelCache.HasInstance ? ParcelCache.Parcels.Count : 0)}");
            sb.AppendLine($"robots,{(RobotEconomicsManager.Instance != null ? RobotEconomicsManager.Instance.RobotStatsMap.Count : 0)}");
            File.WriteAllText(Path.Combine(folderPath, "metadata.csv"), sb.ToString());
        }

        // ──────────────────────────────────────────────
        // Snapshot zilnic — apelat automat la fiecare zi simulata
        // ──────────────────────────────────────────────
        [ContextMenu("Export Snapshot")]
        public void SaveDailySnapshot()
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.currentDay - 1 : 0;

            ExportRobots(day);
            ExportParcels(day);
            ExportDecisions(day);
            ExportEconomy(day);
            ExportWeather(day);
            ExportHistory();

            Debug.Log($"<color=cyan><b>[DataExporter]</b> Run #{simulationRunId} | 6 CSV-uri exportate pentru Ziua {day}.</color>");
        }

        // ──────────────────────────────────────────────
        // Roboți — stare completa per robot
        // ──────────────────────────────────────────────
        private void ExportRobots(int day)
        {
            if (RobotEconomicsManager.Instance == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("sim_run,day,robot_id,name,type,model,zone,posX,posZ,energy_kwh,distance_m,speed,purchase_price,maint_cost,depr_cost,total_cost,revenue,roi,is_idle");

            foreach (var entry in RobotEconomicsManager.Instance.RobotStatsMap)
            {
                Transform t = entry.Key;
                var s = entry.Value;
                if (t == null) continue;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7:F2},{8:F2},{9:F3},{10:F1},{11:F2},{12:F0},{13:F2},{14:F2},{15:F2},{16:F2},{17:F2},{18}",
                    simulationRunId, day, t.GetInstanceID(), t.name, s.type,
                    s.model ?? "", s.zone, t.position.x, t.position.z,
                    s.energykWh, s.distance, s.speed, s.purchasePrice,
                    s.maintenanceCost, s.depreciationCost, s.TotalCost,
                    s.revenueGenerated, s.ROI, s.IsIdle ? 1 : 0));
            }
            File.WriteAllText(Path.Combine(folderPath, $"robots_day_{day}.csv"), sb.ToString());
        }

        // ──────────────────────────────────────────────
        // Parcele — compoziție sol + cultura + recolta
        // Include pH, tip sol, sezon, temperatura pentru ML
        // ──────────────────────────────────────────────
        private void ExportParcels(int day)
        {
            if (!ParcelCache.HasInstance) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("sim_run,day,parcel_id,name,zone,posX,posZ,soil_type,pH,moisture,nitrogen,phosphorus,potassium,quality,season,temperature,weather,variety,growth_pct,stage,harvest_count,harvest_kg,harvest_revenue,seed_cost,net_profit");

            string season = TimeManager.Instance != null ? TimeManager.Instance.GetCurrentSeason().ToString() : "";
            float temperature = Weather.Components.WeatherSystem.Instance != null
                ? Weather.Components.WeatherSystem.Instance.CurrentTemperature : 0f;
            string weather = Weather.Components.WeatherSystem.Instance != null
                ? Weather.Components.WeatherSystem.Instance.CurrentWeather.ToString() : "";

            foreach (var p in ParcelCache.Instance.ParcelsIterator)
            {
                if (p == null) continue;
                Vector3 pos = p.transform.position;
                string zone = p.name.Contains("_") ? p.name.Split('_')[1].Substring(0, 1) : "?";
                float netProfit = p.harvestedRevenue - p.harvestedSeedCost;

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5:F2},{6:F2},{7},{8:F2},{9:F1},{10:F1},{11:F1},{12:F1},{13:F1},{14},{15:F1},{16},{17},{18:F1},{19},{20},{21:F2},{22:F2},{23:F2},{24:F2}",
                    simulationRunId, day, p.GetInstanceID(), p.name, zone,
                    pos.x, pos.z, p.detectedType, p.soilPH, p.soilMoisture,
                    p.nitrogen, p.phosphorus, p.potassium, p.soilQuality,
                    season, temperature, weather,
                    string.IsNullOrEmpty(p.plantedVarietyName) ? "" : p.plantedVarietyName,
                    p.growthProgress, p.currentGrowthStage,
                    p.harvestedCount, p.harvestedWeightKg,
                    p.harvestedRevenue, p.harvestedSeedCost, netProfit));
            }
            File.WriteAllText(Path.Combine(folderPath, $"parcels_day_{day}.csv"), sb.ToString());
        }

        // ──────────────────────────────────────────────
        // Decizii AI — fiecare alternativa evaluata pe rand
        // Scoruri detaliate per factor (pH, N, P, K, umiditate)
        // ──────────────────────────────────────────────
        private void ExportDecisions(int day)
        {
            if (DecisionTracker.Instance == null || RobotEconomicsManager.Instance == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("sim_run,day,decision_id,robot_name,decision_type,chosen_option,parcel,score,net_value,ph_score,nitrogen_score,phosphorus_score,potassium_score,humidity_score,scheduling_value,num_alternatives,timestamp");

            bool hasData = false;

            foreach (var entry in RobotEconomicsManager.Instance.RobotStatsMap)
            {
                if (entry.Key == null) continue;
                int totalCount = DecisionTracker.Instance.GetTotalDecisions(entry.Key);

                // Cate decizii am exportat deja pentru acest robot?
                if (!lastExportedDecisionCount.TryGetValue(entry.Key, out int lastCount))
                    lastCount = 0;

                if (totalCount <= lastCount) continue; // Nimic nou

                // Luam doar deciziile NOI (diferenta)
                int newCount = totalCount - lastCount;
                var recent = DecisionTracker.Instance.GetRecentDecisions(entry.Key, newCount);

                foreach (var d in recent)
                {
                    hasData = true;
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7:F2},{8:F2},{9:F1},{10:F1},{11:F1},{12:F1},{13:F1},{14:F2},{15},{16:F2}",
                        simulationRunId, day, d.globalIndex, entry.Key.name,
                        d.decisionType, d.chosenOption, d.parcelName,
                        d.chosenScore, d.netValue,
                        d.factors.phScore, d.factors.nitrogenScore,
                        d.factors.phosphorusScore, d.factors.potassiumScore,
                        d.factors.humidityScore, d.schedulingValue,
                        d.alternatives != null ? d.alternatives.Count : 0,
                        d.timestamp));
                }

                lastExportedDecisionCount[entry.Key] = totalCount;
            }

            if (hasData)
                File.WriteAllText(Path.Combine(folderPath, $"decisions_day_{day}.csv"), sb.ToString());
        }

        // ──────────────────────────────────────────────
        // Economie — analiza pe varietati
        // ──────────────────────────────────────────────
        private void ExportEconomy(int day)
        {
            CropDatabase db = CropLoader.Load();
            if (db?.crops == null) return;
            EconomicReport report = CropEconomicsCalculator.GetAnalysis(db);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("sim_run,day,variety,frost_resistant,seed_cost_eur,market_price_eur_kg,yield_kg,growth_days,total_plants,harvested,total_seed_cost,revenue,weight_kg,profit,roi_pct,soil_fit_pct,energy_cost,maint_cost,depr_cost");

            foreach (var crop in db.crops)
            {
                if (!report.AnalysisByVariety.TryGetValue(crop.name, out var s)) continue;
                float profit = s.TotalRevenue - s.TotalSeedCost - s.TotalOperationalCost;
                float roi = (s.TotalSeedCost + s.TotalOperationalCost) > 0
                    ? (profit / (s.TotalSeedCost + s.TotalOperationalCost)) * 100f : 0f;

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4:F3},{5:F2},{6:F3},{7},{8},{9},{10:F2},{11:F2},{12:F2},{13:F2},{14:F1},{15:F1},{16:F2},{17:F2},{18:F2}",
                    simulationRunId, day, crop.name, crop.isFrostResistant ? 1 : 0,
                    crop.seedCostEUR, crop.marketPricePerKg, crop.yieldWeightKg, crop.growthDays,
                    s.TotalPlants, s.HarvestedPlants,
                    s.TotalSeedCost, s.TotalRevenue, s.TotalWeightKg,
                    profit, roi, s.AvgSoilCompatibility,
                    s.TotalEnergyCost, s.TotalMaintenanceCost, s.TotalDepreciationCost));
            }
            File.WriteAllText(Path.Combine(folderPath, $"economy_day_{day}.csv"), sb.ToString());
        }

        // ──────────────────────────────────────────────
        // Istorie — evolutia zilnica a profitului
        // ──────────────────────────────────────────────
        private void ExportHistory()
        {
            if (EconomicsHistoryManager.Instance == null) return;
            var hist = EconomicsHistoryManager.Instance.History;
            if (hist.Count == 0) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("sim_run,day,season,total_revenue,total_costs,net_profit,profit_delta,revenue_delta,weight_kg,total_plants");

            foreach (var s in hist)
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F2},{4:F2},{5:F2},{6:F2},{7:F2},{8:F2},{9}",
                    simulationRunId, s.Day, s.SeasonName,
                    s.TotalRevenue, s.TotalCosts, s.NetProfit,
                    s.ProfitDelta, s.RevenueDelta, s.TotalWeightKg, s.TotalPlants));

            File.WriteAllText(Path.Combine(folderPath, "history.csv"), sb.ToString());
        }

        // ──────────────────────────────────────────────
        // Vreme — log orar cu temperaturile si conditiile meteo
        // ──────────────────────────────────────────────
        private void ExportWeather(int day)
        {
            string weatherPath = Path.Combine(folderPath, "weather.csv");

            // Daca nu exista inca fisierul, scriem header-ul
            bool writeHeader = !File.Exists(weatherPath);

            StringBuilder sb = new StringBuilder();
            if (writeHeader)
                sb.AppendLine("sim_run,day,hour,season,weather,temperature,crop_growth_mult,movement_penalty");

            foreach (var w in weatherLog)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F1},{3},{4},{5:F1},{6:F2},{7:F2}",
                    simulationRunId, w.day, w.hour, w.season, w.weather,
                    w.temperature, w.cropGrowthMult, w.movementPenalty));
            }

            File.AppendAllText(weatherPath, sb.ToString());
            weatherLog.Clear();
        }
    }
}
