using UnityEngine;
using System.Collections.Generic;

public static class PlantingPositionGenerator
{
    public static List<Vector3> Generate(Bounds bounds, PlantingConfig config)
    {
        List<Vector3> positions = new List<Vector3>();
        float marginX = bounds.size.x * config.rowMargin;
        float marginZ = bounds.size.z * config.endMargin;
        float minX = bounds.min.x + marginX;
        float maxX = bounds.max.x - marginX;
        float minZ = bounds.min.z + marginZ;
        float maxZ = bounds.max.z - marginZ;

        for (int row = 0; row < config.rowCount; row++)
        {
            float x = CalculateRowX(row, minX, maxX, config.rowCount);
            bool forward = (row % 2 == 0);
            for (int plant = 0; plant < config.plantsPerRow; plant++)
            {
                int plantIndex = GetPlantIndex(plant, forward, config.plantsPerRow);
                float z = CalculatePlantZ(plantIndex, minZ, maxZ, config.plantsPerRow);
                float y = TerrainHelper.GetSurfaceHeight(new Vector3(x, 0, z));
                positions.Add(new Vector3(x, y, z));
            }
        }
        return positions;
    }

    private static float CalculateRowX(int row, float minX, float maxX, int rowCount)
    {
        if (rowCount <= 1)
            return (minX + maxX) * 0.5f;
        float t = (float)row / (rowCount - 1);
        return Mathf.Lerp(minX, maxX, t);
    }

    private static int GetPlantIndex(int plant, bool forward, int plantsPerRow)
    {
        if (forward)
            return plant;
        return plantsPerRow - 1 - plant;
    }

    private static float CalculatePlantZ(int plantIndex, float minZ, float maxZ, int plantsPerRow)
    {
        if (plantsPerRow <= 1)
            return (minZ + maxZ) * 0.5f;
        float t = (float)plantIndex / (plantsPerRow - 1);
        return Mathf.Lerp(minZ, maxZ, t);
    }
}
