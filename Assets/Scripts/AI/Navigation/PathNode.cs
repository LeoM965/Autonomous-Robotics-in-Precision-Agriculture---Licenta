using UnityEngine;
public class PathNode
{
    public int x, y;
    public float worldX, worldZ;
    public bool walkable;
    public float g = float.MaxValue;
    public float h;
    public float f => g + h;
    public PathNode parent;
    public PathNode(int x, int y, float worldX, float worldZ, bool walkable)
    {
        this.x = x;
        this.y = y;
        this.worldX = worldX;
        this.worldZ = worldZ;
        this.walkable = walkable;
    }
    public void Reset()
    {
        g = float.MaxValue;
        h = 0;
        parent = null;
    }
    public Vector3 WorldPosition => new Vector3(worldX, 0, worldZ);
}
