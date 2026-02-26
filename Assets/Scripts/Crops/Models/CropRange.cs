using UnityEngine;
using System;

[Serializable]
public class CropRange
{
    public float min;
    public float max;
    public float optimal;

    public bool Contains(float value)
    {
        return value >= min && value <= max;
    }

    public float GetScore(float value)
    {
        if (value < min || value > max)
            return 0f;

        float distance = Mathf.Abs(value - optimal);
        float maxDistance = Mathf.Max(optimal - min, max - optimal);
        
        if (maxDistance <= 0)
            return 1f;

        return 1f - distance / maxDistance;
    }
}
