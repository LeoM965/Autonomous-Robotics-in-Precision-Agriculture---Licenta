using UnityEngine;
using System.Collections.Generic;
using AI.DataStructures;

namespace AI.Navigation
{
    public class Pathfinder : MonoBehaviour
    {
        public static Pathfinder Instance { get; private set; }
        private PathGrid grid;
        
        private readonly MinHeap<PathNode> openHeap = new MinHeap<PathNode>();
        private readonly HashSet<PathNode> inOpenSet = new HashSet<PathNode>();
        private readonly HashSet<PathNode> closedSet = new HashSet<PathNode>();
        
        private void Awake() => Instance = this;
        
        private void Start() => grid = PathGrid.Instance;
        
        public List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            if (!EnsureGridReady()) return null;
            
            PathNode startNode = grid.GetNode(start);
            PathNode endNode = grid.GetNode(end);
            
            if (startNode == null || endNode == null) return null;
            
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
            
            startNode.g = 0;
            startNode.h = PathHelper.Heuristic(startNode, endNode);
            AddToOpenSet(startNode);
            
            while (!openHeap.IsEmpty)
            {
                PathNode current = openHeap.Dequeue();
                inOpenSet.Remove(current);
                
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
            grid.ResetAllNodes();
            openHeap.Clear();
            inOpenSet.Clear();
            closedSet.Clear();
        }

        private void AddToOpenSet(PathNode node)
        {
            openHeap.Enqueue(node, node.f);
            inOpenSet.Add(node);
        }

        private void ProcessNeighbours(PathNode current, PathNode endNode)
        {
            List<PathNode> neighbours = grid.GetNeighbours(current);
            foreach (var nb in neighbours)
            {
                if (!nb.walkable || closedSet.Contains(nb)) continue;
                
                float moveCost = (nb.x != current.x && nb.y != current.y) ? 1.414f : 1f;
                float newG = current.g + moveCost * grid.CellSize;
                
                if (newG < nb.g)
                {
                    nb.g = newG;
                    nb.h = PathHelper.Heuristic(nb, endNode);
                    nb.parent = current;
                    
                    if (!inOpenSet.Contains(nb))
                    {
                        AddToOpenSet(nb);
                    }
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