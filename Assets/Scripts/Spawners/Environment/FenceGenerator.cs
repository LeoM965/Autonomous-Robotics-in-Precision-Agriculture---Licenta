using UnityEngine;
using System.Collections.Generic;

public class FenceGenerator : MonoBehaviour
{
    public static FenceGenerator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Header("References")]
    [SerializeField] private GameObject fencePrefab;
    [SerializeField] private Terrain terrain;
    [SerializeField] private Transform container;
    [Header("Zones")]
    public FenceZone[] zones;
    [Header("Settings")]
    [SerializeField] [Range(1f, 10f)] private float spacing = 8f;
    [SerializeField] [Range(0f, 5f)] private float heightOffset = 0f;
    private HashSet<int> placedPositions = new HashSet<int>();
    [ContextMenu("Generate")]
    public void Generate()
    {
        if (fencePrefab == null || terrain == null || zones == null)
            return;
        if (container == null)
            container = new GameObject("Fences").transform;
        Clear();
        int totalCount = 0;
        foreach (var zone in zones)
            totalCount += GenerateZone(zone);
        StaticBatchingUtility.Combine(container.gameObject);
        Debug.Log($"Fences generated: {totalCount}");
    }
    private int GenerateZone(FenceZone zone)
    {
        Vector3 corner0 = new Vector3(zone.startXZ.x, 0, zone.startXZ.y);
        Vector3 corner1 = new Vector3(zone.endXZ.x, 0, zone.startXZ.y);
        Vector3 corner2 = new Vector3(zone.endXZ.x, 0, zone.endXZ.y);
        Vector3 corner3 = new Vector3(zone.startXZ.x, 0, zone.endXZ.y);
        int count = 0;
        count += PlaceSide(corner0, corner1, zone, true);
        count += PlaceSide(corner1, corner2, zone, false);
        count += PlaceSide(corner2, corner3, zone, true);
        count += PlaceSide(corner3, corner0, zone, false);
        return count;
    }
    private int PlaceSide(Vector3 start, Vector3 end, FenceZone zone, bool checkGap)
    {
        if (checkGap && zone.hasGap)
        {
            int count = 0;
            if (start.x < zone.gapStart.x)
                count += PlaceLine(start, new Vector3(zone.gapStart.x, start.y, start.z));
            if (end.x > zone.gapEnd.x)
                count += PlaceLine(new Vector3(zone.gapEnd.x, start.y, start.z), end);
            return count;
        }
        return PlaceLine(start, end);
    }
    private int PlaceLine(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        if (distance < 0.1f)
            return 0;
        direction.Normalize();
        int steps = Mathf.Max(1, Mathf.RoundToInt(distance / spacing));
        int count = 0;
        for (int i = 0; i <= steps; i++)
        {
            Vector3 position = Vector3.Lerp(start, end, (float)i / steps);
            position.y = TerrainHelper.GetHeight(position) + heightOffset;
            int hash = SpawnHelper.PositionHash(position);
            if (!placedPositions.Add(hash))
                continue;
            var fence = Instantiate(fencePrefab, position, Quaternion.LookRotation(direction), container);
            fence.tag = "Fence";
            fence.isStatic = true;
            var collider = fence.GetComponent<BoxCollider>();
            if (collider == null)
                collider = fence.AddComponent<BoxCollider>();
            collider.size = new Vector3(3f, 2f, 0.3f);
            collider.center = new Vector3(0, 1f, 0);
            count++;
        }
        return count;
    }
    [ContextMenu("Clear")]
    public void Clear()
    {
        if (container == null)
            return;
        while (container.childCount > 0)
            DestroyImmediate(container.GetChild(0).gameObject);
        placedPositions.Clear();
    }
}
