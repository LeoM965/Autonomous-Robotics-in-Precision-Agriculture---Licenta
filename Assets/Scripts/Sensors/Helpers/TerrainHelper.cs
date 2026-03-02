using UnityEngine;
public static class TerrainHelper
{
    public static float GetHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return 0f;
        return terrain.SampleHeight(position) + terrain.transform.position.y;
    }

    public static float GetSurfaceHeight(Vector3 pos)
    {
        float terrainH = GetHeight(pos);
        Vector3 rayStart = new Vector3(pos.x, terrainH + 10f, pos.z);
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 15f, ~0, QueryTriggerInteraction.Collide))
            return Mathf.Max(hit.point.y, terrainH) + 0.02f;
        return terrainH;
    }
    public static Vector3 GetPosition(float x, float z, float yOffset)
    {
        float y = GetHeight(new Vector3(x, 0, z));
        return new Vector3(x, y + yOffset, z);
    }
    public static bool IsInsideZone(Vector3 position, Vector2 v1, Vector2 v2)
    {
        float minX = Mathf.Min(v1.x, v2.x);
        float maxX = Mathf.Max(v1.x, v2.x);
        float minZ = Mathf.Min(v1.y, v2.y);
        float maxZ = Mathf.Max(v1.y, v2.y);
        return position.x >= minX && position.x <= maxX && position.z >= minZ && position.z <= maxZ;
    }

}
