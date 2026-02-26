using UnityEngine;
using System.Linq;
[System.Serializable]
public class TextureZone
{
    public string name;
    public TerrainLayer layer;
    public Vector2 startXZ;
    public Vector2 endXZ;
}
public class PaintTerrainArea : MonoBehaviour
{
    public TextureZone[] zones;
    [ContextMenu("Paint All Zones")]
    public void PaintAll()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null || zones == null) return;
        TerrainData data = terrain.terrainData;
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;
        float[,,] maps = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);
        int[] layerIndices = new int[zones.Length];
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].layer != null)
                layerIndices[i] = FindLayerIndex(data, zones[i].layer);
            else
                layerIndices[i] = -1;
        }
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = data.size;
        for (int z = 0; z < alphaHeight; z++)
        {
            for (int x = 0; x < alphaWidth; x++)
            {
                float worldX = terrainPos.x + ((float)x / alphaWidth) * terrainSize.x;
                float worldZ = terrainPos.z + ((float)z / alphaHeight) * terrainSize.z;
                Vector3 worldPos = new Vector3(worldX, 0, worldZ);
                bool painted = false;
                for (int i = zones.Length - 1; i >= 0; i--)
                {
                    if (layerIndices[i] == -1) continue;
                    if (TerrainHelper.IsInsideZone(worldPos, zones[i].startXZ, zones[i].endXZ))
                    {
                        int layerIndex = layerIndices[i];
                        for (int l = 0; l < data.alphamapLayers; l++)
                        {
                            maps[z, x, l] = (l == layerIndex) ? 1f : 0f;
                        }
                        painted = true;
                        break;
                    }
                }
            }
        }
        data.SetAlphamaps(0, 0, maps);
        Debug.Log("Terrain painted successfully!");
    }
    private int FindLayerIndex(TerrainData data, TerrainLayer layer)
    {
        for (int i = 0; i < data.terrainLayers.Length; i++)
        {
            if (data.terrainLayers[i] == layer)
                return i;
        }
        return -1;
    }
}
