using UnityEngine;
[System.Serializable]
public class PlantingConfig
{
    [Header("Layout")]
    public int rowCount = 5;
    public float rowMargin = 0.1f;
    public float endMargin = 0.1f;

    [Header("Movement")]
    public float plantDistance = 3f;
    public float arriveDistance = 2.5f;

    [Header("Quality")]
    public float minSoilQuality = 0.3f;
    public float rescanInterval = 15f;
}
