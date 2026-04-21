using UnityEngine;

public class FertileTerrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Terrain terrain;

    [Header("Field Dimensions")]
    [SerializeField] private float fieldSize = 100f;
    [SerializeField] private float maxHeight = 10f;
    [SerializeField] private int plotsPerRow = 8;
    [SerializeField] private float plotBorderWidth = 0.5f;

    [Header("Terrain Relief")]
    [SerializeField][Range(0f, 1f)] private float flatness = 0.85f;
    [SerializeField] private bool addDrainage = true;
    [SerializeField] private int drainageChannels = 2;

    [Header("Soil Colors")]
    [SerializeField] private Color richSoil = new Color(0.28f, 0.20f, 0.12f);
    [SerializeField] private Color dryPatch = new Color(0.42f, 0.34f, 0.25f);
    [SerializeField] private Color moistPatch = new Color(0.22f, 0.15f, 0.08f);
    [SerializeField] private Color border = new Color(0.18f, 0.12f, 0.07f);

    [Header("Settings")]
    [SerializeField] private int textureResolution = 512;
    [SerializeField] private bool autoGenerate = true;

    private TerrainData terrainData;

    void Start()
    {
        InitializeTerrain();

        if (autoGenerate)
            GenerateTerrain();
    }

    void InitializeTerrain()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>() ?? FindObjectOfType<Terrain>();

        if (terrain == null)
            Debug.LogError("Terrain not found! Add Terrain component.");
    }

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain reference missing.");
            return;
        }

        terrainData = terrain.terrainData;
        terrainData.size = new Vector3(fieldSize, maxHeight, fieldSize);

        CreateHeightmap();
        CreateSoilTexture();

        terrain.Flush();
        Debug.Log("Terrain generated successfully");
    }

    void CreateHeightmap()
    {
        int res = terrainData.heightmapResolution;
        float[,] heights = new float[res, res];
        float plotPixels = res / (float)plotsPerRow;

        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                heights[z, x] = CalculateHeight(x, z, res, plotPixels);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    float CalculateHeight(int x, int z, int res, float plotPixels)
    {
        float h = 0.5f;

        h += NoiseLayer(x, z, 0.002f, 0, 0.08f * (1f - flatness));
        h += NoiseLayer(x, z, 0.008f, 50f, 0.04f * (1f - flatness));
        h += NoiseLayer(x, z, 0.04f, 100f, 0.02f);

        if (addDrainage)
            h -= GetDrainageInfluence(x, z, res) * 0.03f;

        h += GetBorderInfluence(x, z, plotPixels) * 0.012f;

        return Mathf.Clamp01(h);
    }

    float NoiseLayer(int x, int z, float scale, float offset, float strength)
    {
        float noise = Mathf.PerlinNoise(x * scale + offset, z * scale + offset);
        return (noise - 0.5f) * strength;
    }

    float GetDrainageInfluence(int x, int z, int res)
    {
        float influence = 0f;
        float spacing = res / (float)(drainageChannels + 1);

        for (int i = 1; i <= drainageChannels; i++)
        {
            float channelPos = i * spacing;
            float dist = Mathf.Abs(z - channelPos);
            float threshold = spacing * 0.15f;

            if (dist < threshold)
            {
                float falloff = 1f - (dist / threshold);
                influence = Mathf.Max(influence, falloff);
            }
        }

        return influence;
    }

    float GetBorderInfluence(int x, int z, float plotPixels)
    {
        float xMod = x % plotPixels;
        float zMod = z % plotPixels;
        float distToBorder = Mathf.Min(xMod, zMod, plotPixels - xMod, plotPixels - zMod);
        float threshold = plotPixels * 0.12f;

        return distToBorder < threshold ? 1f - (distToBorder / threshold) : 0f;
    }

    void CreateSoilTexture()
    {
        int res = textureResolution;
        Color[] colors = new Color[res * res];

        float plotSize = fieldSize / plotsPerRow;
        float pixelsPerMeter = res / fieldSize;
        float plotPixels = plotSize * pixelsPerMeter;
        float borderPixels = plotBorderWidth * pixelsPerMeter;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                colors[y * res + x] = GetPixelColor(x, y, plotPixels, borderPixels);
            }
        }

        ApplyTexture(colors);
    }

    Color GetPixelColor(int x, int y, float plotPixels, float borderPixels)
    {
        float xMod = x % plotPixels;
        float yMod = y % plotPixels;

        bool isBorder = xMod < borderPixels || yMod < borderPixels ||
                       xMod > plotPixels - borderPixels || yMod > plotPixels - borderPixels;

        return isBorder ? border : CalculateSoilColor(x, y);
    }

    Color CalculateSoilColor(int x, int y)
    {
        float wx = (float)x / textureResolution * fieldSize;
        float wy = (float)y / textureResolution * fieldSize;

        float moisture = Mathf.PerlinNoise(wx * 0.25f, wy * 0.25f);
        float compaction = Mathf.PerlinNoise(wx * 0.4f + 100f, wy * 0.4f + 100f);
        float organic = Mathf.PerlinNoise(wx * 0.15f + 200f, wy * 0.15f + 200f);

        Color soil = richSoil;

        if (moisture > 0.7f)
            soil = Color.Lerp(soil, moistPatch, (moisture - 0.7f) / 0.3f);
        else if (moisture < 0.3f)
            soil = Color.Lerp(soil, dryPatch, (0.3f - moisture) / 0.3f);

        if (organic > 0.6f)
            soil = Color.Lerp(soil, soil * 0.85f, (organic - 0.6f) / 0.4f);

        if (compaction > 0.75f)
            soil = Color.Lerp(soil, new Color(0.34f, 0.27f, 0.19f), 0.25f);

        return AddGrain(soil, x, y);
    }

    Color AddGrain(Color soil, int x, int y)
    {
        float grain = Mathf.PerlinNoise(x * 0.015f, y * 0.015f);
        float variance = (grain - 0.5f) * 0.06f;

        soil.r = Mathf.Clamp01(soil.r + variance * 0.2f);
        soil.g = Mathf.Clamp01(soil.g + variance * 0.2f);
        soil.b = Mathf.Clamp01(soil.b + variance * 0.15f);

        return soil;
    }

    void ApplyTexture(Color[] colors)
    {
        Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, true);
        texture.SetPixels(colors);
        texture.Apply();

        TerrainLayer layer = new TerrainLayer
        {
            diffuseTexture = texture,
            tileSize = new Vector2(fieldSize, fieldSize),
            smoothness = 0.08f,
            metallic = 0f
        };

        terrainData.terrainLayers = new TerrainLayer[] { layer };
    }

    void OnDrawGizmos()
    {
        if (terrain == null) return;

        Gizmos.color = Color.green;
        Vector3 center = terrain.transform.position + new Vector3(fieldSize * 0.5f, 0, fieldSize * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(fieldSize, 0.1f, fieldSize));
    }
}