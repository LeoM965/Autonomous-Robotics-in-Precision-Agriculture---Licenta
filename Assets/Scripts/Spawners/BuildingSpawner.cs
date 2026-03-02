using UnityEngine;
using System.Collections.Generic;

public class BuildingSpawner : MonoBehaviour
{
    [SerializeField] GameObject buildingPrefab;
    [SerializeField] GameObject chargingStationPrefab;
    [SerializeField] float buildingScale = 1.5f;
    [SerializeField] float stationScale = 1.2f;
    [SerializeField] float stationOffset = 12f;
    [SerializeField] float margin = 15f;
    [SerializeField] TerrainLayer[] skipLayers;
    
    GameObject root;
    HashSet<string> skipNames = new HashSet<string>();
    List<Building> buildings = new List<Building>();
    
    public static BuildingSpawner Instance { get; private set; }
    public List<Building> spawnedBuildings => buildings;
    
    void Awake() => Instance = this;
    
    void Start()
    {
        if (skipLayers != null)
        {
            foreach (TerrainLayer layer in skipLayers)
            {
                if (layer != null) skipNames.Add(layer.name);
            }
        }
        
        root = SpawnHelper.CreateRoot(transform, "Buildings", true);
        PaintTerrainArea painter = FindFirstObjectByType<PaintTerrainArea>();
        if (painter?.zones == null) return;
        
        foreach (TextureZone zone in painter.zones)
        {
            if (zone.layer != null && skipNames.Contains(zone.layer.name)) continue;
            if (Mathf.Abs(zone.endXZ.x - zone.startXZ.x) < 50) continue;
            if (Mathf.Abs(zone.endXZ.y - zone.startXZ.y) < 50) continue;
            
            float x = Mathf.Min(zone.startXZ.x, zone.endXZ.x) + margin;
            float z = Mathf.Max(zone.startXZ.y, zone.endXZ.y) - margin;
            
            Spawn(buildingPrefab, x, z, buildingScale, 180f, BuildingType.Generic);
            Spawn(chargingStationPrefab, x, z - stationOffset, stationScale, 0f, BuildingType.ChargingStation);
        }
        StaticBatchingUtility.Combine(root);
    }
    
    void Spawn(GameObject prefab, float x, float z, float scale, float rotation, BuildingType type)
    {
        if (prefab == null) return;
        float y = TerrainHelper.GetHeight(new Vector3(x, 0, z));
        GameObject obj = Instantiate(prefab, new Vector3(x, y, z), Quaternion.Euler(0, rotation, 0), root.transform);
        obj.transform.localScale = Vector3.one * scale;
        obj.tag = "Building";
        obj.isStatic = true;
        buildings.Add(new Building(type, new Vector3(x, y, z)));
    }
    
    public static Vector3? GetNearestChargingStation(Vector3 position)
    {
        if (Instance == null) return null;
        
        FenceZone robotZone = ZoneHelper.GetZoneAt(position);
        Vector3? best = null;
        float minDist = float.MaxValue;
        
        foreach (Building b in Instance.buildings)
        {
            if (b.type != BuildingType.ChargingStation) continue;
            FenceZone stationZone = ZoneHelper.GetZoneAt(b.position);
            if (stationZone != robotZone) continue;
            float dist = (b.position - position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                best = b.position;
            }
        }
        return best;
    }
    
    [ContextMenu("Clear")]
    public void Clear()
    {
        SpawnHelper.ClearRoot(transform, "Buildings");
        root = null;
        buildings.Clear();
    }
    
    [ContextMenu("Regenerate")]
    public void Regenerate() { Clear(); Start(); }
}