using UnityEngine;
using System.IO;
using Sensors.Components;
using System.Collections.Generic;

namespace SaveSystem
{
    /// <summary>
    /// Saves/loads full simulation state.
    /// Uses FindObjectsByType directly — no ParcelCache dependency.
    /// </summary>
    public class SimSaveManager : MonoBehaviour
    {
        public static SimSaveManager Instance { get; private set; }
        public static string LastSaveName { get; set; } = "";

        private static string SaveDir => Path.Combine(Application.persistentDataPath, "Saves");
        private static string ExportDir => Path.Combine(Application.dataPath, "..", "Exported_SimData");

        private bool savedOnQuit;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void OnApplicationQuit() => AutoSave();

        private void AutoSave()
        {
            if (savedOnQuit || string.IsNullOrEmpty(LastSaveName)) return;
            savedOnQuit = true;
            Save(LastSaveName);
        }

        // ═══════════════════════════════════════════
        //  SAVE
        // ═══════════════════════════════════════════

        public void Save(string saveName)
        {
            var data = new SimSaveData
            {
                saveName = saveName,
                savedAt = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            // Time — flush any pending robot-accumulated time first
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.FlushPendingTime();
                data.totalSimulatedHours = TimeManager.Instance.totalSimulatedHours;
                data.dayNumber = TimeManager.Instance.currentDay;
            }

            // Weather
            var weather = Weather.Components.WeatherSystem.Instance;
            if (weather != null)
            {
                data.weatherType = weather.CurrentWeather.ToString();
                data.temperature = weather.CurrentTemperature;
            }

            // Economics
            var eco = Economics.Managers.RobotEconomicsManager.Instance;
            if (eco != null)
            {
                data.globalEnergyCost = eco.GlobalEnergykWh;
                data.globalMaintenanceCost = eco.GlobalMaintenanceCost;
                data.globalDepreciationCost = eco.GlobalDepreciationCost;
            }

            // Daily history
            var histMgr = Economics.Managers.EconomicsHistoryManager.Instance;
            if (histMgr != null)
                data.dailyHistory = new System.Collections.Generic.List<Economics.Models.DailySnapshot>(histMgr.History);

            // Crop rotation history (accumulated across seasons)
            foreach (var kvp in EnvironmentalSensor.CropHistory)
            {
                data.cropHistory.Add(new CropHistorySave
                {
                    variety = kvp.Key,
                    totalPlants = kvp.Value.totalPlants,
                    totalRevenue = kvp.Value.totalRevenue,
                    totalWeightKg = kvp.Value.totalWeightKg,
                    totalSeedCost = kvp.Value.totalSeedCost
                });
            }

            // Parcels + Crops (exact positions)
            foreach (var s in FindObjectsByType<EnvironmentalSensor>(FindObjectsSortMode.None))
            {
                if (s == null) continue;
                var ps = new ParcelSave
                {
                    name = s.name,
                    plantedVariety = s.plantedVarietyName ?? "",
                    lastHarvestedVariety = s.lastHarvestedVarietyName ?? "",
                    harvestedCount = s.harvestedCount,
                    harvestedWeightKg = s.harvestedWeightKg,
                    harvestedRevenue = s.harvestedRevenue,
                    harvestedSeedCost = s.harvestedSeedCost
                };

                if (s.composition != null)
                {
                    ps.moisture = s.composition.moisture;
                    ps.pH = s.composition.pH;
                    ps.nitrogen = s.composition.nitrogen;
                    ps.phosphorus = s.composition.phosphorus;
                    ps.potassium = s.composition.potassium;
                    ps.irrigationRate = s.composition.irrigationRate;
                }

                // Save each crop with its exact world position
                foreach (var crop in s.activeCrops)
                {
                    if (crop == null || crop.IsBeingHarvested) continue;
                    ps.crops.Add(new CropSave
                    {
                        posX = crop.transform.position.x,
                        posY = crop.transform.position.y,
                        posZ = crop.transform.position.z,
                        rotY = crop.transform.eulerAngles.y,
                        progress = crop.Progress,
                        purchasePrice = crop.PurchasePrice
                    });
                }

                data.parcels.Add(ps);
            }

            // Robots
            foreach (var r in FindObjectsByType<RobotEnergy>(FindObjectsSortMode.None))
            {
                var rs = new RobotSave
                {
                    name = r.name,
                    posX = r.transform.position.x,
                    posY = r.transform.position.y,
                    posZ = r.transform.position.z,
                    rotY = r.transform.eulerAngles.y,
                    batteryKWh = r.CurrentBattery
                };

                if (eco != null && eco.RobotStatsMap.TryGetValue(r.transform, out var stats))
                {
                    rs.distance = stats.distance;
                    rs.time = stats.time;
                    rs.energykWh = stats.energykWh;
                    rs.maintenanceCost = stats.maintenanceCost;
                    rs.depreciationCost = stats.depreciationCost;
                    rs.revenueGenerated = stats.revenueGenerated;
                }

                if (AI.Analytics.DecisionTracker.Instance != null)
                {
                    var decisions = AI.Analytics.DecisionTracker.Instance.GetRecentDecisions(r.transform, 100);
                    if (decisions != null)
                        rs.decisions = new System.Collections.Generic.List<AI.Analytics.DecisionRecord>(decisions);
                }

                data.robots.Add(rs);
            }

            Directory.CreateDirectory(SaveDir);
            File.WriteAllText(Path.Combine(SaveDir, saveName + ".json"), JsonUtility.ToJson(data, true));
            LastSaveName = saveName;

            int totalCrops = 0;
            foreach (var p in data.parcels) totalCrops += p.crops.Count;
            Debug.Log($"[Save] \"{saveName}\" — Ziua {data.dayNumber}, " +
                      $"{data.parcels.Count} parcele, {data.robots.Count} roboți, {totalCrops} plante");
        }

        // ═══════════════════════════════════════════
        //  LOAD
        // ═══════════════════════════════════════════

        public void Load(string saveName)
        {
            string path = Path.Combine(SaveDir, saveName + ".json");
            if (!File.Exists(path)) { Debug.LogWarning($"[Save] \"{saveName}\" nu există!"); return; }

            var data = JsonUtility.FromJson<SimSaveData>(File.ReadAllText(path));
            var db = CropLoader.Load();

            // Time + sync all time-dependent systems
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.totalSimulatedHours = data.totalSimulatedHours;
                TimeManager.Instance.ClearPendingTime(); // discard stale pending hours from pre-load frames
            }
            if (CropManager.Instance != null)
                CropManager.Instance.SyncProcessTime(data.totalSimulatedHours);

            // Weather — exact type + temperature
            var weather = Weather.Components.WeatherSystem.Instance;
            if (weather != null && !string.IsNullOrEmpty(data.weatherType))
            {
                if (System.Enum.TryParse<Weather.Models.WeatherType>(data.weatherType, out var wt))
                    weather.RestoreState(wt, data.temperature);
            }

            // Economics — restore costs + sync time
            var eco = Economics.Managers.RobotEconomicsManager.Instance;
            if (eco != null)
            {
                eco.RestoreCosts(data.globalEnergyCost, data.globalMaintenanceCost, data.globalDepreciationCost);
                eco.SyncTime(data.totalSimulatedHours);
            }

            // Daily history
            var histMgr = Economics.Managers.EconomicsHistoryManager.Instance;
            if (histMgr != null)
                histMgr.RestoreHistory(data.dailyHistory);

            // Crop rotation history
            EnvironmentalSensor.CropHistory.Clear();
            if (data.cropHistory != null)
            {
                foreach (var ch in data.cropHistory)
                {
                    EnvironmentalSensor.CropHistory[ch.variety] = new Sensors.Components.HistoricalCropRecord
                    {
                        totalPlants = ch.totalPlants,
                        totalRevenue = ch.totalRevenue,
                        totalWeightKg = ch.totalWeightKg,
                        totalSeedCost = ch.totalSeedCost
                    };
                }
            }

            // Parcels — soil + harvest stats + crops at exact positions
            var map = new Dictionary<string, ParcelSave>();
            foreach (var ps in data.parcels) map[ps.name] = ps;

            int totalCropsSpawned = 0;
            var settings = Resources.Load<CropSettings>("CropSettings");

            foreach (var s in FindObjectsByType<EnvironmentalSensor>(FindObjectsSortMode.None))
            {
                if (s == null || !map.ContainsKey(s.name)) continue;
                var ps = map[s.name];

                // Soil
                if (s.composition != null)
                {
                    s.composition.moisture = ps.moisture;
                    s.composition.pH = ps.pH;
                    s.composition.nitrogen = ps.nitrogen;
                    s.composition.phosphorus = ps.phosphorus;
                    s.composition.potassium = ps.potassium;
                    s.composition.irrigationRate = ps.irrigationRate;
                }

                s.plantedVarietyName = ps.plantedVariety;
                s.lastHarvestedVarietyName = ps.lastHarvestedVariety;
                s.RestoreHarvestStats(ps.harvestedCount, ps.harvestedWeightKg, ps.harvestedRevenue, ps.harvestedSeedCost);
                s.Analyze();

                // Re-spawn crops at exact saved positions
                if (db != null && ps.crops.Count > 0 && !string.IsNullOrEmpty(ps.plantedVariety))
                {
                    int cropIndex = db.GetIndex(ps.plantedVariety);
                    if (cropIndex >= 0)
                    {
                        var cropData = db.crops[cropIndex];
                        var prefab = CropLoader.LoadPrefab(cropData.prefabPath);
                        if (prefab != null)
                        {
                            foreach (var cs in ps.crops)
                            {
                                var pos = new Vector3(cs.posX, cs.posY, cs.posZ);
                                var rot = Quaternion.Euler(0, cs.rotY, 0);

                                var plant = CropPool.Instance != null
                                    ? CropPool.Instance.Get(prefab, pos, rot, s.transform)
                                    : Object.Instantiate(prefab, pos, rot, s.transform);

                                var growth = plant.GetComponent<CropGrowth>();
                                if (growth == null)
                                {
                                    growth = plant.AddComponent<CropGrowth>();
                                    if (settings) growth.settings = settings;
                                    var scaler = plant.GetComponent<CropVisualScaling>();
                                    if (scaler && settings) scaler.settings = settings;
                                    var harvest = plant.GetComponent<CropHarvestVisuals>();
                                    if (harvest && settings) harvest.settings = settings;
                                }

                                float nRate = cropData.nitrogenConsumptionRate / Mathf.Max(1, ps.crops.Count);
                                float nOpt = cropData.requirements?.nitrogen?.optimal ?? -1f;
                                growth.Initialize(cropData.GrowthHours, cs.purchasePrice, nRate, nOpt, cropIndex);

                                // Restore exact progress
                                if (cs.progress > 0f)
                                {
                                    float elapsed = cs.progress * cropData.GrowthHours;
                                    growth.ApplyJobResults(elapsed, 0f);
                                }

                                s.activeCrops.Add(growth);
                                totalCropsSpawned++;
                            }
                        }
                    }
                }
            }

            // Robots — position + battery
            foreach (var r in FindObjectsByType<RobotEnergy>(FindObjectsSortMode.None))
            {
                foreach (var rs in data.robots)
                {
                    if (rs.name == r.name)
                    {
                        r.transform.position = new Vector3(rs.posX, rs.posY, rs.posZ);
                        r.transform.eulerAngles = new Vector3(0, rs.rotY, 0);
                        r.SetBattery(rs.batteryKWh);
                        
                        // Restore per-robot stats
                        if (eco != null)
                        {
                            eco.RegisterRobot(r.transform);
                            eco.RobotStatsMap[r.transform].Restore(
                                rs.distance, rs.time, rs.energykWh, 
                                rs.maintenanceCost, rs.depreciationCost, rs.revenueGenerated);
                        }

                        // Restore decisions
                        if (AI.Analytics.DecisionTracker.Instance != null && rs.decisions != null)
                        {
                            AI.Analytics.DecisionTracker.Instance.RestoreDecisions(r.transform, rs.decisions);
                        }

                        break;
                    }
                }
            }

            LastSaveName = saveName;
            savedOnQuit = false;
            Debug.Log($"[Save] Restaurat \"{saveName}\" — Ziua {data.dayNumber}, " +
                      $"{data.parcels.Count} parcele, {data.robots.Count} roboți, " +
                      $"{totalCropsSpawned} plante");
        }

        // ═══════════════════════════════════════════
        //  UI HELPERS
        // ═══════════════════════════════════════════

        public static string[] GetSaveNames()
        {
            if (!Directory.Exists(SaveDir)) return new string[0];
            var files = Directory.GetFiles(SaveDir, "*.json");
            var names = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            System.Array.Sort(names);
            return names;
        }

        public static SimSaveData PeekSave(string saveName)
        {
            string path = Path.Combine(SaveDir, saveName + ".json");
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<SimSaveData>(File.ReadAllText(path)); }
            catch { return null; }
        }

        public static void DeleteSave(string saveName)
        {
            string savePath = Path.Combine(SaveDir, saveName + ".json");
            if (File.Exists(savePath)) File.Delete(savePath);

            string exportPath = Path.Combine(ExportDir, saveName);
            if (Directory.Exists(exportPath)) Directory.Delete(exportPath, true);

            Debug.Log($"[Save] Șters: \"{saveName}\"");
        }
    }
}
