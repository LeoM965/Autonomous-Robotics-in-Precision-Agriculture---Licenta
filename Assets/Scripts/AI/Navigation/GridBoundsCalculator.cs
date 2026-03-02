using UnityEngine;

namespace AI.Navigation
{
    public static class GridBoundsCalculator
    {
        public static Rect Calculate(FenceZone[] zones, Terrain terrain)
        {
            float minX = 0, maxX = 200, minZ = 0, maxZ = 200;

            if (zones != null && zones.Length > 0)
            {
                minX = minZ = float.MaxValue;
                maxX = maxZ = float.MinValue;
                foreach (FenceZone z in zones)
                {
                    minX = Mathf.Min(minX, z.startXZ.x);
                    minZ = Mathf.Min(minZ, z.startXZ.y);
                    maxX = Mathf.Max(maxX, z.endXZ.x);
                    maxZ = Mathf.Max(maxZ, z.endXZ.y);
                }
            }
            else if (terrain != null)
            {
                maxX = terrain.terrainData.size.x;
                maxZ = terrain.terrainData.size.z;
            }

            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }
    }
}
