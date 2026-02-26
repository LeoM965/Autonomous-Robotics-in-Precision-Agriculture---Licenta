using System;

[Serializable]
public class CropRequirements
{
    public CropRange pH;
    public CropRange humidity;
    public CropRange nitrogen;
    public CropRange phosphorus;
    public CropRange potassium;
}
