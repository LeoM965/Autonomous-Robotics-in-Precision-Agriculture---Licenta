using UnityEngine;
[System.Serializable]
public class SpawnConfig
{
    [Header("Quantities")]
    public int countPerType = 4;
    public float spacing = 12f;
    [Header("Validation")]
    public float minRoadWeight = 0.3f;
    public int maxAttempts = 100;
    [Header("Height")]
    public float heightOffset = 0.5f;
}
