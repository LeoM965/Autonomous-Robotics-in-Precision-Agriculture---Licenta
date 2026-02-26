using UnityEngine;

[CreateAssetMenu(fileName = "CropSettings", menuName = "Robotics/Crop Settings")]
public class CropSettings : ScriptableObject
{
    [Header("Planting Requirements")]
    public float minQualityToPlant = 30f;

    [Header("Growth Visuals")]
    public float[] stageScales = { 0.01f, 0.4f, 0.7f, 1f };
    public float[] thresholds = { 0f, 0.25f, 0.5f, 0.85f, 1f };

    [Header("Harvest Visuals")]
    public float sinkDuration = 1f;
    public float sinkDepth = 0.5f;
}
