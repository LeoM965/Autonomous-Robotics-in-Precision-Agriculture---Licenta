using UnityEngine;
using System.Collections.Generic;
public class MapData
{
    public FenceZone[] zones;
    public List<Transform> robots = new List<Transform>();
    public List<Building> buildings = new List<Building>();
    public Vector3 terrainPosition;
    public float inverseX;
    public float inverseZ;
    public int selectedRobotIndex = -1;
    public void Initialize(Terrain terrain)
    {
        if (terrain == null)
            return;
        Vector3 size = terrain.terrainData.size;
        terrainPosition = terrain.transform.position;
        if (size.x > 0) inverseX = 1f / size.x;
        if (size.z > 0) inverseZ = 1f / size.z;
    }
    public void LoadRobots(MultiRobotSpawner spawner)
    {
        if (spawner == null)
            return;
        List<GameObject> spawned = spawner.GetRobots();
        robots.Clear();
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                robots.Add(spawned[i].transform);
        }
    }
    public void LoadZones(FenceGenerator fence)
    {
        if (fence != null)
            zones = fence.zones;
    }
    public void LoadBuildings(BuildingSpawner spawner)
    {
        if (spawner != null)
            buildings = spawner.spawnedBuildings;
    }
}
