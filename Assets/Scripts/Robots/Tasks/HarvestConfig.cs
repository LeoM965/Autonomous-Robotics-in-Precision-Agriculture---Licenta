using UnityEngine;

[System.Serializable]
public class HarvestConfig
{
    [Header("Movement")]
    public float arriveDistance = 2.5f;
    public float harvestRadius = 1.8f;

    [Header("Timing")]
    public float harvestDelay = 0.5f;
    public float rescanInterval = 5f;

    [Header("Quality")]
    public float minSoilQuality = 0f;
}
