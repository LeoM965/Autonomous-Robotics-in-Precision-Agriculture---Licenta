using UnityEngine;
using System.Collections.Generic;

public static class PathHelper
{
    public static List<Vector3> SimplifyPath(List<Vector3> path, PathGrid grid)
    {
        if (path.Count < 3) return path;
        
        List<Vector3> simplified = new List<Vector3>();
        simplified.Add(path[0]);
        int current = 0;
        
        while (current < path.Count - 1)
        {
            int furthest = current + 1;
            for (int i = path.Count - 1; i > current; i--)
            {
                if (HasLineOfSight(path[current], path[i], grid))
                {
                    furthest = i;
                    break;
                }
            }
            simplified.Add(path[furthest]);
            current = furthest;
        }
        return simplified;
    }
    
    public static bool HasLineOfSight(Vector3 start, Vector3 end, PathGrid grid)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        int steps = Mathf.CeilToInt(dist / (grid.CellSize * 0.25f));
        
        for (int i = 1; i < steps; i++)
        {
            float t = (float)i / steps;
            Vector3 point = Vector3.Lerp(start, end, t);
            PathNode node = grid.GetNode(point);
            if (node == null || !node.walkable)
                return false;
        }
        return true;
    }
    
    public static List<Vector3> BuildPath(PathNode end, PathGrid grid)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode node = end;
        
        while (node != null)
        {
            float height = grid.GetTerrainHeight(node.WorldPosition);
            Vector3 point = new Vector3(node.worldX, height, node.worldZ);
            path.Add(point);
            node = node.parent;
        }
        path.Reverse();
        return path;
    }
    
    public static float Heuristic(PathNode a, PathNode b)
    {
        float dx = Mathf.Abs(a.worldX - b.worldX);
        float dz = Mathf.Abs(a.worldZ - b.worldZ);
        return dx + dz;
    }
}
