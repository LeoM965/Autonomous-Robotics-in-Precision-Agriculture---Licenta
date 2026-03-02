using UnityEngine;
using System.Collections.Generic;

namespace AI.Navigation
{
    public static class PathHelper
    {
        public static List<Vector3> SimplifyPath(List<Vector3> path, PathGrid grid)
        {
            if (path.Count < 3) return path;
            
            List<Vector3> simplified = new List<Vector3> { path[0] };
            int currentIdx = 0;
            
            while (currentIdx < path.Count - 1)
            {
                int furthestVisibleIdx = FindFurthestVisible(path, currentIdx, grid);
                simplified.Add(path[furthestVisibleIdx]);
                currentIdx = furthestVisibleIdx;
            }
            return simplified;
        }

        private static int FindFurthestVisible(List<Vector3> path, int fromIdx, PathGrid grid)
        {
            for (int i = path.Count - 1; i > fromIdx + 1; i--)
            {
                if (HasLineOfSight(path[fromIdx], path[i], grid))
                    return i;
            }
            return fromIdx + 1;
        }
        
        public static bool HasLineOfSight(Vector3 start, Vector3 end, PathGrid grid)
        {
            Vector3 dir = end - start;
            float dist = dir.magnitude;
            float stepSize = grid.CellSize * 0.25f;
            int steps = Mathf.CeilToInt(dist / stepSize);
            
            for (int i = 1; i < steps; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, (float)i / steps);
                PathNode node = grid.GetNode(point);
                if (node == null || !node.walkable) return false;
            }
            return true;
        }
        
        public static List<Vector3> BuildPath(PathNode end, PathGrid grid)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode current = end;
            
            while (current != null)
            {
                float height = grid.GetTerrainHeight(current.WorldPosition);
                path.Add(new Vector3(current.worldX, height, current.worldZ));
                current = current.parent;
            }
            path.Reverse();
            return path;
        }
        
        public static float Heuristic(PathNode a, PathNode b)
        {
            return Mathf.Abs(a.worldX - b.worldX) + Mathf.Abs(a.worldZ - b.worldZ);
        }
    }
}
