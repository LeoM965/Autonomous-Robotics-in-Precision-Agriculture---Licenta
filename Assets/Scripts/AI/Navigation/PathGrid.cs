using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

namespace AI.Navigation
{
    public class PathGrid : MonoBehaviour
    {
        public static PathGrid Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private float obstacleRadius = 6f;
        
        private PathNode[,] grid;
        private int width, height;
        private float originX, originZ;
        private Terrain terrain;
        
        private readonly List<PathNode> neighbourCache = new List<PathNode>(8);
        private static readonly Collider[] blockCheckBuffer = new Collider[32];
        
        public float CellSize => cellSize;
        public bool IsReady => grid != null;
        
        private void Awake() => Instance = this;
        
        private void Start()
        {
            terrain = Terrain.activeTerrain;
            Invoke(nameof(Build), 2f);
        }
        
        private void Build()
        {
            FenceGenerator fence = FindFirstObjectByType<FenceGenerator>();
            Rect bounds = GridBoundsCalculator.Calculate(fence?.zones, terrain);
            
            originX = bounds.xMin;
            originZ = bounds.yMin;
            width = Mathf.CeilToInt(bounds.width / cellSize);
            height = Mathf.CeilToInt(bounds.height / cellSize);
            
            InitializeGrid();
            Debug.Log($"[PathGrid] Built {width}x{height} grid.");
        }

        private void InitializeGrid()
        {
            grid = new PathNode[width, height];
            float halfCell = cellSize * 0.5f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = GetWorldPosAt(x, y, halfCell);
                    bool walkable = !IsBlocked(worldPos);
                    grid[x, y] = new PathNode(x, y, worldPos.x, worldPos.z, walkable);
                }
            }
        }

        private Vector3 GetWorldPosAt(int x, int y, float halfCell)
        {
            float wx = originX + x * cellSize + halfCell;
            float wz = originZ + y * cellSize + halfCell;
            float h = terrain != null ? terrain.SampleHeight(new Vector3(wx, 0, wz)) + 1f : 1f;
            return new Vector3(wx, h, wz);
        }
        
        private bool IsBlocked(Vector3 pos)
        {
            int count = Physics.OverlapSphereNonAlloc(pos, obstacleRadius, blockCheckBuffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (blockCheckBuffer[i].CompareTag("Fence")) return true;
                if (blockCheckBuffer[i].GetComponent<EnvironmentalSensor>() != null) return true;
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
                    if (nb != null && IsNeighbourValid(node, dx, dy))
                    {
                        neighbourCache.Add(nb);
                    }
                }
            }
            return neighbourCache;
        }

        private bool IsNeighbourValid(PathNode node, int dx, int dy)
        {
            if (dx == 0 || dy == 0) return true; // Straight neighbours
            
            // Diagonal check: both adjacent non-diagonal nodes must be walkable
            PathNode adjX = GetNode(node.x + dx, node.y);
            PathNode adjY = GetNode(node.x, node.y + dy);
            return adjX != null && adjX.walkable && adjY != null && adjY.walkable;
        }
        
        public PathNode FindNearestWalkable(PathNode from)
        {
            const int searchRadius = 15;
            for (int r = 1; r < searchRadius; r++)
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
}