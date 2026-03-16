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

        // Accumulated harvest stats (persist after plants are destroyed)
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
            composition.moisture = Mathf.Min(composition.moisture + amount, 100f);
            Analyze();
        }

        public void RemoveCrop(CropGrowth crop)
        {
            activeCrops.Remove(crop);
            if (activeCrops.Count == 0) plantedVarietyName = "";
        }
    }
}
