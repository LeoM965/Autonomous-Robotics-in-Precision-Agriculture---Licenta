using UnityEngine;
using System.Collections.Generic;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance { get; private set; }
    private PathGrid grid;
    
    // Reusable collections to avoid GC allocations
    private readonly MinHeap<PathNode> openHeap = new MinHeap<PathNode>();
    private readonly HashSet<PathNode> inOpenSet = new HashSet<PathNode>();
    private readonly HashSet<PathNode> closedSet = new HashSet<PathNode>();
    
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        grid = PathGrid.Instance;
    }
    
    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        if (grid == null || !grid.IsReady)
        {
            grid = PathGrid.Instance;
            if (grid == null || !grid.IsReady)
                return null;
        }
        
        PathNode startNode = grid.GetNode(start);
        PathNode endNode = grid.GetNode(end);
        
        if (startNode == null || endNode == null)
            return null;
        
        Vector3 originalEnd = end;
        bool endWasBlocked = !endNode.walkable;
        
        if (endWasBlocked)
            endNode = grid.FindNearestWalkable(endNode);
        
        if (endNode == null)
            return null;
        
        grid.ResetAllNodes();
        
        // Clear and reuse cached collections
        openHeap.Clear();
        inOpenSet.Clear();
        closedSet.Clear();
        
        startNode.g = 0;
        startNode.h = PathHelper.Heuristic(startNode, endNode);
        openHeap.Enqueue(startNode, startNode.f);
        inOpenSet.Add(startNode);
        
        while (!openHeap.IsEmpty)
        {
            PathNode current = openHeap.Dequeue();
            inOpenSet.Remove(current);
            
            if (current == endNode)
            {
                List<Vector3> result = PathHelper.SimplifyPath(PathHelper.BuildPath(endNode, grid), grid);
                if (endWasBlocked)
                    result.Add(originalEnd);
                return result;
            }
            
            closedSet.Add(current);
            List<PathNode> neighbours = grid.GetNeighbours(current);
            
            for (int i = 0; i < neighbours.Count; i++)
            {
                PathNode nb = neighbours[i];
                
                if (!nb.walkable)
                    continue;
                if (closedSet.Contains(nb))
                    continue;
                
                bool diagonal = (nb.x != current.x && nb.y != current.y);
                float moveCost = diagonal ? 1.414f : 1f;
                float cost = current.g + moveCost * grid.CellSize;
                
                if (cost < nb.g)
                {
                    nb.g = cost;
                    nb.h = PathHelper.Heuristic(nb, endNode);
                    nb.parent = current;
                    
                    if (!inOpenSet.Contains(nb))
                    {
                        openHeap.Enqueue(nb, nb.f);
                        inOpenSet.Add(nb);
                    }
                }
            }
        }
        return null;
    }
}