using UnityEngine;
public static class SpawnHelper
{
    public static Vector3 RandomPositionInZone(Vector2 zoneMin, Vector2 zoneMax, float margin)
    {
        float x = Random.Range(zoneMin.x + margin, zoneMax.x - margin);
        float z = Random.Range(zoneMin.y + margin, zoneMax.y - margin);
        return new Vector3(x, 0, z);
    }
    public static Quaternion RandomYRotation()
    {
        float angle = Random.Range(0f, 360f);
        return Quaternion.Euler(0, angle, 0);
    }
    public static int PositionHash(Vector3 position, float precision)
    {
        int x = Mathf.RoundToInt(position.x * precision);
        int y = Mathf.RoundToInt(position.y * precision);
        int z = Mathf.RoundToInt(position.z * precision);
        int hash = x * 73856093;
        hash = hash ^ (y * 19349663);
        hash = hash ^ (z * 83492791);
        return hash;
    }
    public static int PositionHash(Vector3 position)
    {
        return PositionHash(position, 2f);
    }
}
