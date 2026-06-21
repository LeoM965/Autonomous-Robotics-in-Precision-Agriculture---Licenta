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

        int plantsPerRow = Settings.SimulationSettings.PlantsPerRow;

        for (int row = 0; row < config.rowCount; row++)
        {
            float x = CalculateRowX(row, minX, maxX, config.rowCount);
            bool forward = (row % 2 == 0);
            for (int plant = 0; plant < plantsPerRow; plant++)
            {
                int plantIndex = GetPlantIndex(plant, forward, plantsPerRow);
                float z = CalculatePlantZ(plantIndex, minZ, maxZ, plantsPerRow);
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
    public static List<Vector3> GenerateZigzag(Bounds bounds, float spacing, float margin, float fixedY)
    {
        List<Vector3> positions = new List<Vector3>();
        float minX = bounds.min.x + margin;
        float maxX = bounds.max.x - margin;
        float minZ = bounds.min.z + margin;
        float maxZ = bounds.max.z - margin;

        int rowCount = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ) / spacing) + 1);
        for (int row = 0; row < rowCount; row++)
        {
            float z = rowCount <= 1 ? (minZ + maxZ) * 0.5f : Mathf.Lerp(minZ, maxZ, (float)row / (rowCount - 1));
            if (row % 2 == 0)
            {
                positions.Add(new Vector3(minX, fixedY, z));
                positions.Add(new Vector3(maxX, fixedY, z));
            }
            else
            {
                positions.Add(new Vector3(maxX, fixedY, z));
                positions.Add(new Vector3(minX, fixedY, z));
            }
        }

        if (positions.Count == 0)
            positions.Add(new Vector3(bounds.center.x, fixedY, bounds.center.z));

        return positions;
    }

    /// <summary>
    /// Verifică dacă parcela are deja plante pe toate pozițiile generate.
    /// Returnează true dacă parcela este complet plantată, false dacă mai sunt locuri goale.
    /// </summary>
    public static bool IsParcelFullyPlanted(Sensors.Components.EnvironmentalSensor parcel, PlantingConfig config)
    {
        if (parcel == null) return true;
        if (parcel.activeCrops.Count == 0) return false;

        Collider col = parcel.GetComponent<Collider>();
        if (col == null) return true;

        var positions = Generate(col.bounds, config);
        if (positions.Count == 0) return true;

        // Colectăm pozițiile plantelor active
        var cropPositions = new List<Vector3>();
        foreach (var crop in parcel.activeCrops)
        {
            if (crop != null) cropPositions.Add(crop.transform.position);
        }

        // Verificăm câte din pozițiile generate au deja o plantă în apropiere
        int occupied = 0;
        float threshold = 0.5f * 0.5f; // 0.5m distanță, comparăm sqr
        foreach (var pos in positions)
        {
            foreach (var cp in cropPositions)
            {
                float dx = pos.x - cp.x, dz = pos.z - cp.z;
                if (dx * dx + dz * dz < threshold)
                {
                    occupied++;
                    break;
                }
            }
        }

        return occupied >= positions.Count;
    }

    /// <summary>
    /// Filtrează pozițiile de plantare, returnând doar cele care nu au deja o plantă în apropiere.
    /// </summary>
    public static List<Vector3> FilterUnplantedPositions(List<Vector3> positions, Sensors.Components.EnvironmentalSensor parcel)
    {
        if (parcel == null || parcel.activeCrops.Count == 0) return positions;

        var cropPositions = new List<Vector3>();
        foreach (var crop in parcel.activeCrops)
        {
            if (crop != null) cropPositions.Add(crop.transform.position);
        }

        var filtered = new List<Vector3>();
        float threshold = 0.5f * 0.5f;
        foreach (var pos in positions)
        {
            bool hasNearbyCrop = false;
            foreach (var cp in cropPositions)
            {
                float dx = pos.x - cp.x, dz = pos.z - cp.z;
                if (dx * dx + dz * dz < threshold)
                {
                    hasNearbyCrop = true;
                    break;
                }
            }
            if (!hasNearbyCrop) filtered.Add(pos);
        }

        return filtered;
    }
}
