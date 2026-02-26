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
        [Header("AgroPhysics Data")]
        public SoilComposition composition;
        public AgroSoilType detectedType;
        
        [Header("Crop Context")]
        public HashSet<CropGrowth> activeCrops = new HashSet<CropGrowth>();
        public string plantedVarietyId;
        public string plantedVarietyName;

        [Header("Cache & Management")]
        public int zoneIndex = -1;
        public bool isScheduledForTask;

        private TerrainAnalyzer _analyzer;
        private SensorVisuals _visuals;
        private SoilAnalysis _latestAnalysis;

        public SoilAnalysis LatestAnalysis => _latestAnalysis;

        private void Awake()
        {
            _analyzer = GetComponent<TerrainAnalyzer>();
            _visuals = GetComponent<SensorVisuals>();

            InitializeSoil();
        }

        private void InitializeSoil()
        {
            detectedType = _analyzer.AnalyzeTerrain(transform.position);
            composition = SoilCompositionGenerator.Generate(detectedType);
            Analyze();
        }


        public void Analyze()
        {
            if (composition == null) return;
            _latestAnalysis = SoilAnalysisService.Analyze(composition);
            _visuals.Refresh(_latestAnalysis);
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
            if (activeCrops.Remove(crop))
            {
                if (activeCrops.Count == 0)
                {
                    plantedVarietyId = null;
                    plantedVarietyName = null;
                }
            }
        }
    }
}
