using UnityEngine;
public class SpawnValidator
{
    private float[,,] alphaMap;
    private int layerIndex;
    private int mapWidth;
    private int mapHeight;
    private Vector3 terrainSize;
    private float minWeight;
    public SpawnValidator(Terrain terrain, TerrainLayer targetLayer, float minWeight)
    {
        this.minWeight = minWeight;
        if (terrain == null || targetLayer == null)
            return;
        TerrainData data = terrain.terrainData;
        layerIndex = System.Array.IndexOf(data.terrainLayers, targetLayer);
        if (layerIndex < 0)
            return;
        mapWidth = data.alphamapWidth;
        mapHeight = data.alphamapHeight;
        terrainSize = data.size;
        alphaMap = data.GetAlphamaps(0, 0, mapWidth, mapHeight);
    }
    public bool IsValidPosition(Vector3 position)
    {
        if (alphaMap == null)
            return true;
        float normalizedX = position.x / terrainSize.x;
        float normalizedZ = position.z / terrainSize.z;
        int x = Mathf.Clamp((int)(normalizedX * mapWidth), 0, mapWidth - 1);
        int z = Mathf.Clamp((int)(normalizedZ * mapHeight), 0, mapHeight - 1);
        float weight = alphaMap[z, x, layerIndex];
        return weight > minWeight;
    }
}
