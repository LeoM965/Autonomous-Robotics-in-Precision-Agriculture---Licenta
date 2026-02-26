using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class PathGrid : MonoBehaviour
{
    public static PathGrid Instance { get; private set; }
    [SerializeField] float cellSize = 2f;
    [SerializeField] float obstacleRadius = 6f;
    
    PathNode[,] grid;
    int width, height;
    float originX, originZ;
    Terrain terrain;
    
    private readonly List<PathNode> neighbourCache = new List<PathNode>(8);
    
    public float CellSize => cellSize;
    public bool IsReady => grid != null;
    
    void Awake() => Instance = this;
    
    void Start()
    {
        terrain = Terrain.activeTerrain;
        Invoke(nameof(Build), 2f);
    }
    
    void Build()
    {
        FenceGenerator fence = FindFirstObjectByType<FenceGenerator>();
        float minX = 0, maxX = 200, minZ = 0, maxZ = 200;
        
        if (fence?.zones != null && fence.zones.Length > 0)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;
            foreach (FenceZone z in fence.zones)
            {
                if (z.startXZ.x < minX) minX = z.startXZ.x;
                if (z.startXZ.y < minZ) minZ = z.startXZ.y;
                if (z.endXZ.x > maxX) maxX = z.endXZ.x;
                if (z.endXZ.y > maxZ) maxZ = z.endXZ.y;
            }
        }
        else if (terrain != null)
        {
            maxX = terrain.terrainData.size.x;
            maxZ = terrain.terrainData.size.z;
        }
        
        originX = minX;
        originZ = minZ;
        width = Mathf.CeilToInt((maxX - minX) / cellSize);
        height = Mathf.CeilToInt((maxZ - minZ) / cellSize);
        grid = new PathNode[width, height];
        
        float half = cellSize * 0.5f;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float wx = originX + x * cellSize + half;
                float wz = originZ + y * cellSize + half;
                float h = terrain != null ? terrain.SampleHeight(new Vector3(wx, 0, wz)) + 1f : 1f;
                Vector3 pos = new Vector3(wx, h, wz);
                bool blocked = IsBlocked(pos);
                grid[x, y] = new PathNode(x, y, wx, wz, !blocked);
            }
        }
        Debug.Log("[PathGrid] Built " + width + "x" + height + " grid");
    }
    
    private static readonly Collider[] _blockCheckBuffer = new Collider[32];
    
    bool IsBlocked(Vector3 pos)
    {
        int count = Physics.OverlapSphereNonAlloc(pos, obstacleRadius, _blockCheckBuffer, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            if (_blockCheckBuffer[i].CompareTag("Fence")) return true;
            if (_blockCheckBuffer[i].GetComponent<EnvironmentalSensor>() != null) return true;
        }
        return false;
    }
    
    public PathNode GetNode(Vector3 pos)
    {
        if (grid == null) return null;
        int x = Mathf.Clamp((int)((pos.x - originX) / cellSize), 0, width - 1);
        int y = Mathf.Clamp((int)((pos.z - originZ) / cellSize), 0, height - 1);
        return grid[x, y];
    }
    
    public PathNode GetNode(int x, int y)
    {
        if ((uint)x >= width || (uint)y >= height) return null;
        return grid[x, y];
    }
    
    public List<PathNode> GetNeighbours(PathNode node)
    {
        neighbourCache.Clear();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                PathNode nb = GetNode(node.x + dx, node.y + dy);
                if (nb == null) continue;
                if (dx != 0 && dy != 0)
                {
                    PathNode adjX = GetNode(node.x + dx, node.y);
                    PathNode adjY = GetNode(node.x, node.y + dy);
                    if (adjX == null || !adjX.walkable || adjY == null || !adjY.walkable)
                        continue;
                }
                neighbourCache.Add(nb);
            }
        }
        return neighbourCache;
    }
    
    public PathNode FindNearestWalkable(PathNode from)
    {
        for (int r = 1; r < 15; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    PathNode node = GetNode(from.x + dx, from.y + dy);
                    if (node != null && node.walkable) return node;
                }
            }
        }
        return null;
    }
    
    public void ResetAllNodes()
    {
        if (grid == null) return;
        foreach (PathNode n in grid) n.Reset();
    }
    
    public float GetTerrainHeight(Vector3 pos) => terrain != null ? terrain.SampleHeight(pos) : 0;
}
