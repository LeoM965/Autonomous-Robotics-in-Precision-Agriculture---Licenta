using UnityEngine;
using System.Collections.Generic;
using AI.DataStructures;

namespace AI.Navigation
{
    public class Pathfinder : MonoBehaviour
    {
        public static Pathfinder Instance { get; private set; }
        private PathGrid grid;
        private int currentSearchId;
        
        private readonly MinHeap<PathNode> openHeap = new MinHeap<PathNode>();
        private readonly HashSet<PathNode> closedSet = new HashSet<PathNode>();
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }
        
        private void Start() => grid = PathGrid.Instance;
        
        public List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            if (!EnsureGridReady()) return null;
            
            PathNode startNode = grid.GetNode(start);
            PathNode endNode = grid.GetNode(end);
            
            if (startNode == null || endNode == null) return null;
            
            if (!startNode.walkable)
            {
                PathNode nearest = grid.FindNearestWalkable(startNode);
                if (nearest != null && Vector3.Distance(start, nearest.WorldPosition) < Vector3.Distance(start, end))
                {
                    startNode = nearest;
                }
            }
            
            Vector3 originalEnd = end;
            bool endWasBlocked = !endNode.walkable;
            if (endWasBlocked)
            {
                endNode = grid.FindNearestWalkable(endNode);
                if (endNode == null) return null;
            }
            
            return ExecuteAStar(startNode, endNode, originalEnd, endWasBlocked);
        }

        private bool EnsureGridReady()
        {
            if (grid == null || !grid.IsReady)
                grid = PathGrid.Instance;
            return grid != null && grid.IsReady;
        }

        private List<Vector3> ExecuteAStar(PathNode startNode, PathNode endNode, Vector3 originalEnd, bool endWasBlocked)
        {
            PrepareSearch();
            
            EnsureNodeReady(startNode);
            startNode.g = 0;
            startNode.h = PathHelper.Heuristic(startNode, endNode);
            AddToOpenSet(startNode);
            
            while (!openHeap.IsEmpty)
            {
                PathNode current = openHeap.Dequeue();
                
                // Lazy duplicate: daca nodul a fost deja procesat cu un g mai bun, il ignoram
                if (closedSet.Contains(current)) continue;
                
                if (current == endNode)
                {
                    return FinalizePath(endNode, originalEnd, endWasBlocked);
                }
                
                closedSet.Add(current);
                ProcessNeighbours(current, endNode);
            }
            
            return null;
        }

        private void PrepareSearch()
        {
            currentSearchId++;
            openHeap.Clear();
            closedSet.Clear();
        }

        private void EnsureNodeReady(PathNode node)
        {
            if (node.lastSearchId != currentSearchId)
            {
                node.Reset();
                node.lastSearchId = currentSearchId;
            }
        }

        private void AddToOpenSet(PathNode node)
        {
            openHeap.Enqueue(node, node.f);
        }

        private void ProcessNeighbours(PathNode current, PathNode endNode)
        {
            List<PathNode> neighbours = grid.GetNeighbours(current);
            foreach (var nb in neighbours)
            {
                if (!nb.walkable || closedSet.Contains(nb)) continue;
                
                EnsureNodeReady(nb);
                float moveCost = (nb.x != current.x && nb.y != current.y) ? 1.414f : 1f;
                float newG = current.g + moveCost * grid.CellSize;
                
                if (newG < nb.g)
                {
                    nb.g = newG;
                    nb.h = PathHelper.Heuristic(nb, endNode);
                    nb.parent = current;
                    
                    AddToOpenSet(nb);
                }
            }
        }

        private List<Vector3> FinalizePath(PathNode endNode, Vector3 originalEnd, bool endWasBlocked)
        {
            List<Vector3> result = PathHelper.SimplifyPath(PathHelper.BuildPath(endNode, grid), grid);
            if (endWasBlocked) result.Add(originalEnd);
            return result;
        }
    }
}