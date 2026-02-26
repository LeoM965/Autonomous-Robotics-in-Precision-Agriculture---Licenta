using UnityEngine;

public class CropVisualScaling : MonoBehaviour
{
    public CropSettings settings;

    public CropStage DetermineStage(float progress)
    {
        if (settings == null) return CropStage.Seed;
        if (progress < settings.thresholds[1]) return CropStage.Seed;
        if (progress < settings.thresholds[2]) return CropStage.Seedling;
        if (progress < settings.thresholds[3]) return CropStage.Growing;
        return CropStage.Mature;
    }

    public float CalculateScale(CropStage stage, float progress)
    {
        if (settings == null) return 1f;
        int idx = (int)stage;
        
        float[] scales = settings.stageScales;
        float currentScale = scales[idx];
        float nextScale = stage < CropStage.Mature ? scales[idx + 1] : currentScale;
        
        float stageProgress = GetStageProgress(idx, progress);
        return currentScale + (nextScale - currentScale) * stageProgress;
    }

    private float GetStageProgress(int idx, float progress)
    {
        float[] thresholds = settings.thresholds;
        if (idx >= thresholds.Length - 1) return 1f;

        float start = thresholds[idx];
        float end = thresholds[idx + 1];
        float range = end - start;
        if (range <= 0.0001f) return 1f;

        return Mathf.Clamp01((progress - start) / range);
    }

    public float GetInitialScale() => settings != null ? settings.stageScales[0] : 0.01f;
}
