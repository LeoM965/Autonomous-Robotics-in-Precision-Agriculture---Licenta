using UnityEngine;
[System.Serializable]
public class PlantingConfig
{
    [Header("Grid Layout")]
    public int rowCount = 5;
    public int plantsPerRow = 4;
    [Header("Margins")]
    public float rowMargin = 0.1f;
    public float endMargin = 0.1f;
    [Header("Movement")]
    public float arriveDistance = 8f;
    public float plantDistance = 3f;
    [Header("Quality")]
    public float minSoilQuality = 30f;
}
