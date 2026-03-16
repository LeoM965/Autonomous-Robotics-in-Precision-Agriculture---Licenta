using UnityEngine;
using Sensors.Components;
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
        var hits = Physics.RaycastAll(new Vector3(pos.x, terrainH + 10, pos.z), Vector3.down, 15f);
        float bestY = terrainH;
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<EnvironmentalSensor>() != null)
                bestY = Mathf.Max(bestY, hit.point.y);
        }
        return bestY + 0.02f;
    }
    public static Vector3 GetPosition(float x, float z, float yOffset)
    {
        return new Vector3(x, GetSurfaceHeight(new Vector3(x, 0, z)) + yOffset, z);
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
