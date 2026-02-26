using System;

[Serializable]
public class CropData
{
    public string id;
    public string name;
    public string category;
    public string prefabPath;
    public CropRequirements requirements;
    public float seedCostEUR;
    public float yieldValueEUR;
    public int growthDays;
    public float growthTimeSeconds;
}
