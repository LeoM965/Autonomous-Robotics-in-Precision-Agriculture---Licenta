using UnityEngine;
using System.Collections.Generic;
using Sensors.Models;
using Sensors.Services;

namespace Sensors.Components
{
    [RequireComponent(typeof(TerrainAnalyzer))]
    [RequireComponent(typeof(SensorVisuals))]
    public class EnvironmentalSensor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SoilSettings settings;

        [Header("AgroPhysics Data")]
        public SoilComposition composition;
        public AgroSoilType detectedType;
        
        [Header("Crop Context")]
        public HashSet<CropGrowth> activeCrops = new HashSet<CropGrowth>();
        public string plantedVarietyName;

        [Header("Cache & Management")]
        public int zoneIndex = -1;
        public bool isScheduledForTask;

        private TerrainAnalyzer analyzer;
        private SensorVisuals visuals;
        private SoilAnalysis latestAnalysis;

        public SoilAnalysis LatestAnalysis => latestAnalysis;
        public float soilQuality => latestAnalysis.qualityScore;
        public float soilMoisture => composition?.moisture ?? 0f;
        public float soilPH => composition?.pH ?? 0f;
        public float nitrogen => composition?.nitrogen ?? 0f;
        public float phosphorus => composition?.phosphorus ?? 0f;
        public float potassium => composition?.potassium ?? 0f;

        public CropStage currentGrowthStage {
            get {
                foreach(var c in activeCrops)
                 if(c != null) return c.CurrentStage;
                return CropStage.Seed;
            }
        }
        public float growthProgress {
            get {
                foreach(var c in activeCrops)
                 if(c != null) return c.Progress * 100f;
                return 0f;
            }
        }

        // Statistici curente per cultura activa
        public int harvestedCount { get; private set; }
        public float harvestedWeightKg { get; private set; }
        public float harvestedRevenue { get; private set; }
        public float harvestedSeedCost { get; private set; }

        public SoilSettings Settings
        {
            get
            {
                if (settings == null)
                    settings = Resources.Load<SoilSettings>("SoilSettings");
                return settings;
            }
        }

        private void Awake()
        {
            analyzer = GetComponent<TerrainAnalyzer>();
            visuals = GetComponent<SensorVisuals>();
            InitializeSoil();
        }

        private void OnValidate()
        {
            if (settings == null) settings = Resources.Load<SoilSettings>("SoilSettings");
        }

        private void InitializeSoil()
        {
            detectedType = analyzer.AnalyzeTerrain(transform.position);
            composition = SoilCompositionGenerator.Generate(detectedType, Settings);
            Analyze();
        }

        public void Analyze()
        {
            if (composition == null || Settings == null) return;
            
            latestAnalysis = SoilAnalysisService.Analyze(composition, Settings);
            if (visuals != null) visuals.Refresh(latestAnalysis);
        }

        private void OnEnable()
        {
            if (ParcelCache.Instance != null)
                ParcelCache.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (ParcelCache.HasInstance)
                ParcelCache.Instance.Unregister(this);
        }

        public void RecordHarvest(float weightKg, float revenue, float seedCost)
        {
            harvestedCount++;
            harvestedWeightKg += weightKg;
            harvestedRevenue += revenue;
            harvestedSeedCost += seedCost;
        }

        // Registru global istoric per cultura (se acumuleaza la fiecare replantare)
        public static Dictionary<string, HistoricalCropRecord> CropHistory { get; private set; } = new Dictionary<string, HistoricalCropRecord>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticData()
        {
            CropHistory.Clear();
        }

        public void ResetHarvestStats()
        {
            // Salveaza datele sezonului anterior in registrul istoric per cultura
            if (!string.IsNullOrEmpty(plantedVarietyName) && (harvestedCount > 0 || harvestedRevenue > 0))
            {
                if (!CropHistory.ContainsKey(plantedVarietyName))
                    CropHistory[plantedVarietyName] = new HistoricalCropRecord();
                
                var record = CropHistory[plantedVarietyName];
                record.totalPlants += harvestedCount;
                record.totalRevenue += harvestedRevenue;
                record.totalWeightKg += harvestedWeightKg;
                record.totalSeedCost += harvestedSeedCost;
            }

            // Reseteaza complet pentru noua cultura
            harvestedCount = 0;
            harvestedWeightKg = 0;
            harvestedRevenue = 0;
            harvestedSeedCost = 0;
        }

        public void AdjustNutrients(float n, float p, float k)
        {
            if (composition == null) return;
            composition.nitrogen = Mathf.Clamp(composition.nitrogen + n, 0f, 1000f);
            composition.phosphorus = Mathf.Clamp(composition.phosphorus + p, 0f, 1000f);
            composition.potassium = Mathf.Clamp(composition.potassium + k, 0f, 1000f);
            Analyze();
        }

        public void AdjustMoisture(float amount)
        {
            if (composition == null) return;
            composition.moisture = Mathf.Clamp(composition.moisture + amount, 0f, 100f);
            Analyze();
        }

        public void RemoveCrop(CropGrowth crop)
        {
            activeCrops.Remove(crop);
            if (activeCrops.Count == 0) plantedVarietyName = "";
        }

        public static void ResetAllScheduling()
        {
            if (ParcelCache.Instance == null) return;
            foreach (var parcel in ParcelCache.Instance.ParcelsIterator)
            {
                if (parcel != null) parcel.isScheduledForTask = false;
            }
            Debug.Log("[EnvironmentalSensor] Toate programarile sarcinilor au fost resetate.");
        }
    }
}
