using UnityEngine;
using System.Collections.Generic;
public class SpawnPositionFinder
{
    private HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>();
    private SpawnValidator validator;
    private int cellSize;
    private int maxAttempts;
    public SpawnPositionFinder(SpawnValidator validator, float spacing, int maxAttempts)
    {
        this.validator = validator;
        this.cellSize = Mathf.RoundToInt(spacing);
        this.maxAttempts = maxAttempts;
    }
    public Vector3 FindInZone(FenceZone zone, float spacing)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 position = SpawnHelper.RandomPositionInZone(zone.startXZ, zone.endXZ, spacing);
            Vector2Int cell = PositionToCell(position);
            bool cellFree = !usedCells.Contains(cell);
            bool validTerrain = validator == null || validator.IsValidPosition(position);
            if (cellFree && validTerrain)
                return position;
        }
        return GetZoneCenter(zone);
    }
    public void MarkUsed(Vector3 position)
    {
        usedCells.Add(PositionToCell(position));
    }
    public void Clear()
    {
        usedCells.Clear();
    }
    private Vector2Int PositionToCell(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / cellSize);
        int z = Mathf.FloorToInt(position.z / cellSize);
        return new Vector2Int(x, z);
    }
    private Vector3 GetZoneCenter(FenceZone zone)
    {
        float x = (zone.startXZ.x + zone.endXZ.x) * 0.5f;
        float z = (zone.startXZ.y + zone.endXZ.y) * 0.5f;
        return new Vector3(x, 0, z);
    }
}
