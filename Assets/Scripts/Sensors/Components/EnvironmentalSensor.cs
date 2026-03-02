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

        public void RemoveCrop(CropGrowth crop)
        {
            if (activeCrops.Remove(crop) && activeCrops.Count == 0)
                plantedVarietyName = null;
        }
    }
}
