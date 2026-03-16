using UnityEngine;
using System;

[Serializable]
public class CropRange
{
    public float min;
    public float max;
    public float optimal;

    public float GetScore(float value)
    {
        return GetScore(value, min, max, optimal);
    }

    public static float GetScore(float value, float min, float max, float optimal)
    {
        float rangeWidth = Mathf.Max(max - min, 1.0f);
        float buffer = rangeWidth * 0.4f;

        // 1. Inside the core range [min, max]
        if (value >= min && value <= max)
        {
            if (value < optimal)
            {
                // Scale from 0.6 (at min) to 1.0 (at optimal)
                float d = optimal - min;
                if (d <= 0.001f) return 1f;
                return 0.6f + (0.4f * (value - min) / d);
            }
            else
            {
                // Scale from 1.0 (at optimal) down to 0.6 (at max)
                float d = max - optimal;
                if (d <= 0.001f) return 1f;
                return 1.0f - (0.4f * (value - optimal) / d);
            }
        }

        // 2. Below min (Soft Buffer)
        if (value < min && value >= min - buffer)
        {
            // Scale from 0.0 (at min-buffer) to 0.6 (at min)
            if (buffer <= 0.001f) return 0f;
            return 0.6f * (value - (min - buffer)) / buffer;
        }
        
        // 3. Above max (Soft Buffer)
        if (value > max && value <= max + buffer)
        {
            // Scale from 0.6 (at max) down to 0.0 (at max+buffer)
            if (buffer <= 0.001f) return 0f;
            return 0.6f * ((max + buffer) - value) / buffer;
        }

        return 0f;
    }
}
