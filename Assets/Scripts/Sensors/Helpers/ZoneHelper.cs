using UnityEngine;
public static class ZoneHelper
{
    public static FenceZone GetZoneAt(Vector3 position)
    {
        FenceGenerator fence = Object.FindFirstObjectByType<FenceGenerator>();
        if (fence == null || fence.zones == null || fence.zones.Length == 0)
            return null;
        for (int i = 0; i < fence.zones.Length; i++)
        {
            FenceZone zone = fence.zones[i];
            if (TerrainHelper.IsInsideZone(position, zone.startXZ, zone.endXZ))
                return zone;
        }
        return null; // Return null if not in any specific zone
    }
    public static bool IsInZone(Vector3 position, FenceZone zone)
    {
        if (zone == null)
            return false;
        return TerrainHelper.IsInsideZone(position, zone.startXZ, zone.endXZ);
    }
}
