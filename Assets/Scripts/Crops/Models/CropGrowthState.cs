using UnityEngine;
using System;

[Serializable]
public class CropGrowthState
{
    public CropStage stage;
    public float elapsed;
    public float progress;
    public bool isBeingHarvested;
    public float growthTime = -1f;
    public Vector3 baseScale;
    public bool initialized;
}
