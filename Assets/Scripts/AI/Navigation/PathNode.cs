using UnityEngine;

namespace AI.Navigation
{
    public class PathNode
    {
        public int x, y;
        public float worldX, worldZ;
        public bool walkable;
        public PathNode parent;
        public float g, h;
        public int lastSearchId = -1;
        public float f => g + h;
        
        public Vector3 WorldPosition => new Vector3(worldX, 0, worldZ);
        
        public PathNode(int x, int y, float wx, float wz, bool walkable)
        {
            this.x = x;
            this.y = y;
            this.worldX = wx;
            this.worldZ = wz;
            this.walkable = walkable;
            Reset();
        }
        
        public void Reset()
        {
            g = float.MaxValue;
            h = 0;
            parent = null;
        }
    }
}
