using System;

[Serializable]
public class CropData
{
    public string name;
    public string prefabPath;
    public CropRequirements requirements;
    public float seedCostEUR;
    public int growthDays;

    public float GrowthHours => growthDays * 24f;
}
