using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class ParcelSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject parcelPrefab;
    [SerializeField] GameObject signPrefab;
    [Header("Layout")]
    [SerializeField] float spacing = 120f;
    [SerializeField] float padding = 40f;
    [SerializeField] float minZoneSize = 60f;
    [SerializeField] float scale = 4f;
    [Header("Skip Layers")]
    [SerializeField] TerrainLayer[] skipLayers;
    Terrain terrain;
    TerrainData terrainData;
    GameObject root;
    int[] skipIndices;
    float[,,] alphaMap;
    int alphaW, alphaH;
    int zoneIndex;
    int parcelInZone;
    void Start()
    {
        terrain = FindFirstObjectByType<Terrain>();
        if (terrain == null || parcelPrefab == null) return;
        Clear();
        terrainData = terrain.terrainData;
        alphaW = terrainData.alphamapWidth; //pixeli de textura
        alphaH = terrainData.alphamapHeight;
        CacheSkipLayers();
        root = SpawnHelper.CreateRoot(transform, "Parcels");
        SpawnParcels();
        StaticBatchingUtility.Combine(root);
        if (FindFirstObjectByType<ParcelCache>() == null)
        {
            var cacheGo = new GameObject("ParcelCache");
            cacheGo.AddComponent<ParcelCache>();
        }
    }
    void CacheSkipLayers()
    {
        if (skipLayers == null || skipLayers.Length == 0) return;
        var indices = new List<int>();
        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
            foreach (var layer in skipLayers)
                if (layer != null && terrainData.terrainLayers[i].name == layer.name)
                    { indices.Add(i); break; }
        skipIndices = indices.ToArray();
        if (skipIndices.Length > 0)
            alphaMap = terrainData.GetAlphamaps(0, 0, alphaW, alphaH);
    }
    bool ShouldSkip(float x, float z)
    {
        if (alphaMap == null || skipIndices == null || skipIndices.Length == 0)
            return false;
        float nx = (x - terrain.transform.position.x) / terrainData.size.x;
        float nz = (z - terrain.transform.position.z) / terrainData.size.z;
        if (nx < 0 || nx > 1 || nz < 0 || nz > 1) return true;
        int mapX = Mathf.Clamp((int)(nx * (alphaW - 1)), 0, alphaW - 1);
        int mapZ = Mathf.Clamp((int)(nz * (alphaH - 1)), 0, alphaH - 1);
        foreach (int idx in skipIndices)
            if (alphaMap[mapZ, mapX, idx] > 0.2f) return true;
        return false;
    }
    bool ShouldSkipZone(TextureZone zone)
    {
        if (zone.layer == null || skipLayers == null) return false;
        foreach (var l in skipLayers)
            if (l != null && l.name == zone.layer.name) return true;
        return false;
    }
    void SpawnParcels()
    {
        var painter = FindFirstObjectByType<PaintTerrainArea>();
        if (painter?.zones == null) return;
        zoneIndex = 0;
        int total = 0;
        foreach (var zone in painter.zones)
        {
            int count = SpawnInZone(zone);
            if (count > 0) zoneIndex++;
            total += count;
        }
        Debug.Log($"Parcels spawned: {total} in {zoneIndex} zones");
    }
    int SpawnInZone(TextureZone zone)
    {
        float w = Mathf.Abs(zone.endXZ.x - zone.startXZ.x);
        float h = Mathf.Abs(zone.endXZ.y - zone.startXZ.y);
        if (w < minZoneSize || h < minZoneSize || ShouldSkipZone(zone)) return 0;
        float x1 = Mathf.Min(zone.startXZ.x, zone.endXZ.x) + padding;
        float x2 = Mathf.Max(zone.startXZ.x, zone.endXZ.x) - padding;
        float z1 = Mathf.Min(zone.startXZ.y, zone.endXZ.y) + padding;
        float z2 = Mathf.Max(zone.startXZ.y, zone.endXZ.y) - padding;
        float zw = x2 - x1, zh = z2 - z1;
        if (zw < 20 || zh < 20) return 0;
        int cols = Mathf.Max(1, (int)(zw / spacing));
        int rows = Mathf.Max(1, (int)(zh / spacing));
        float stepX = zw / cols, stepZ = zh / rows;
        parcelInZone = 0;
        int count = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float px = x1 + (c + 0.5f) * stepX, pz = z1 + (r + 0.5f) * stepZ;
                if (!ShouldSkip(px, pz)) { SpawnParcel(px, pz); count++; }
            }
        return count;
    }
    string GetZoneLetter() => ((char)('A' + zoneIndex)).ToString();
    void SpawnParcel(float x, float z)
    {
        parcelInZone++;
        string label = $"{GetZoneLetter()}{parcelInZone}";
        float y = TerrainHelper.GetHeight(new Vector3(x, 0, z));
        var p = Instantiate(parcelPrefab, new Vector3(x, y, z), Quaternion.identity, root.transform);
        p.name = $"Parcel_{label}";
        p.transform.localScale = Vector3.one * scale;
        p.isStatic = true;
        foreach (var col in p.GetComponentsInChildren<Collider>())
        {
            if (col is MeshCollider mc) mc.convex = true;
            col.isTrigger = true;
        }
        if (p.GetComponent<EnvironmentalSensor>() == null)
            p.AddComponent<EnvironmentalSensor>();
        SpawnSign(p.transform, label);
    }
    void SpawnSign(Transform parcel, string label)
    {
        Vector3 labelPos = new Vector3(0, 3f, 0);
        SpawnHelper.CreateTextLabel(parcel, label, labelPos);
    }
    [ContextMenu("Clear")]
    public void Clear()
    {
        SpawnHelper.ClearRoot(transform, "Parcels");
        root = null;
        zoneIndex = 0;
        parcelInZone = 0;
    }
    [ContextMenu("Regenerate")]
    public void Regenerate() { Clear(); Start(); }
}
