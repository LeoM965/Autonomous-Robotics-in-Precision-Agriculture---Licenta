using UnityEngine;
public static class ZoneHelper
{
    private static FenceGenerator cachedFence;

    private static FenceGenerator GetFence()
    {
        if (cachedFence == null)
            cachedFence = Object.FindFirstObjectByType<FenceGenerator>();
        return cachedFence;
    }

    public static FenceZone GetZoneAt(Vector3 position)
    {
        var fence = GetFence();
        if (fence == null || fence.zones == null || fence.zones.Length == 0)
            return null;
        for (int i = 0; i < fence.zones.Length; i++)
        {
            FenceZone zone = fence.zones[i];
            if (TerrainHelper.IsInsideZone(position, zone.startXZ, zone.endXZ))
                return zone;
        }
        return null;
    }
    public static bool IsInZone(Vector3 position, FenceZone zone)
    {
        if (zone == null)
            return false;
        return TerrainHelper.IsInsideZone(position, zone.startXZ, zone.endXZ);
    }
}
