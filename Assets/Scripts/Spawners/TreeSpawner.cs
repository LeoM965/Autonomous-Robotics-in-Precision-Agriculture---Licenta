using UnityEngine;
public class TreeSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject[] treePrefabs;
    [Header("Settings")]
    [SerializeField] float treeScale = 2.5f;
    [SerializeField] float edgeMargin = 10f;
    [SerializeField] float spacing = 25f;
    [SerializeField] int treesPerCorner = 2;
    [SerializeField] float avoidRadius = 35f;
    Terrain terrain;
    GameObject root;
    Transform[] buildings;
    void Start()
    {
        terrain = FindFirstObjectByType<Terrain>();
        if (terrain == null || treePrefabs == null || treePrefabs.Length == 0) return;
        Invoke(nameof(SpawnAll), 0.5f);
    }
    void SpawnAll()
    {
        var buildingRoot = GameObject.Find("Buildings");
        if (buildingRoot != null)
        {
            buildings = new Transform[buildingRoot.transform.childCount];
            for (int i = 0; i < buildings.Length; i++)
                buildings[i] = buildingRoot.transform.GetChild(i);
        }
        else
        {
            buildings = new Transform[0];
        }
        Debug.Log($"TreeSpawner: Found {buildings.Length} buildings to avoid");
        root = SpawnHelper.CreateRoot(transform, "Trees", true);
        var pos = terrain.transform.position;
        var size = terrain.terrainData.size;
        float x1 = pos.x + edgeMargin, x2 = pos.x + size.x - edgeMargin;
        float z1 = pos.z + edgeMargin, z2 = pos.z + size.z - edgeMargin;
        SpawnLine(x1, z1, x1, z2);
        SpawnLine(x2, z1, x2, z2);
        SpawnCorner(x1, z1); SpawnCorner(x1, z2);
        SpawnCorner(x2, z1); SpawnCorner(x2, z2);
        StaticBatchingUtility.Combine(root);
    }
    void SpawnLine(float x1, float z1, float x2, float z2)
    {
        int count = Mathf.Max(2, (int)(Vector2.Distance(new Vector2(x1, z1), new Vector2(x2, z2)) / spacing));
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            TrySpawn(Mathf.Lerp(x1, x2, t) + Random.Range(-3f, 3f), Mathf.Lerp(z1, z2, t) + Random.Range(-3f, 3f));
        }
    }
    void SpawnCorner(float x, float z)
    {
        for (int i = 0; i < treesPerCorner; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(3f, 8f);
            TrySpawn(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r);
        }
    }
    void TrySpawn(float x, float z)
    {
        float sqr = avoidRadius * avoidRadius;
        foreach (var b in buildings)
        {
            if (b == null) continue;
            float dx = x - b.position.x;
            float dz = z - b.position.z;
            if (dx * dx + dz * dz < sqr) return;
        }
        var prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
        if (prefab == null) return;
        float y = TerrainHelper.GetHeight(new Vector3(x, 0, z));
        var tree = Instantiate(prefab, new Vector3(x, y, z), Quaternion.Euler(0, Random.Range(0f, 360f), 0), root.transform);
        tree.transform.localScale = Vector3.one * treeScale * Random.Range(0.9f, 1.1f);
        tree.isStatic = true;
    }
    [ContextMenu("Clear")]
    public void Clear() { SpawnHelper.ClearRoot(transform, "Trees"); root = null; }
    [ContextMenu("Regenerate")]
    public void Regenerate() { Clear(); Start(); }
}
