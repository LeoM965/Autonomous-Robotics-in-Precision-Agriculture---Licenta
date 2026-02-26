using UnityEngine;
using Sensors.Models;
using Sensors.Services;

namespace Sensors.Components
{
    public class TerrainAnalyzer : MonoBehaviour
    {
        public AgroSoilType AnalyzeTerrain(Vector3 position)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return AgroSoilType.Standard;

            var td = terrain.terrainData;
            float nx = (position.x - terrain.transform.position.x) / td.size.x;
            float nz = (position.z - terrain.transform.position.z) / td.size.z;

            int mapX = Mathf.Clamp((int)(nx * td.alphamapWidth), 0, td.alphamapWidth - 1);
            int mapZ = Mathf.Clamp((int)(nz * td.alphamapHeight), 0, td.alphamapHeight - 1);

            float[,,] alphas = td.GetAlphamaps(mapX, mapZ, 1, 1);
            int dominantIndex = 0;
            float maxWeight = 0;

            for (int i = 0; i < td.terrainLayers.Length; i++)
            {
                if (alphas[0, 0, i] > maxWeight)
                {
                    maxWeight = alphas[0, 0, i];
                    dominantIndex = i;
                }
            }

            string layerName = td.terrainLayers[dominantIndex].name;
            return MapLayerToSoilType(layerName);
        }

        private AgroSoilType MapLayerToSoilType(string layerName)
        {
            if (string.IsNullOrEmpty(layerName)) return AgroSoilType.Standard;
            
            string lowerName = layerName.ToLower();
            if (lowerName.Contains("fertil")) return AgroSoilType.HighlyFertile;
            if (lowerName.Contains("noroios") || lowerName.Contains("mud")) return AgroSoilType.Waterlogged;
            if (lowerName.Contains("nisipos") || lowerName.Contains("sand")) return AgroSoilType.SandyArid;
            if (lowerName.Contains("chimic") || lowerName.Contains("toxic")) return AgroSoilType.ChemicallyImbalanced;
            
            return AgroSoilType.Standard;
        }
    }
}
