using System;

[Serializable]
public class CropSettingsData
{
    public float minQualityToPlant = 30f;
}

[Serializable]
public class CropDatabase
{
    public CropData[] crops;
    public CropSettingsData settings;

    public CropData Get(string name)
    {
        if (crops == null) return null;
        for (int i = 0; i < crops.Length; i++)
            if (crops[i].name == name) return crops[i];
        return null;
    }

    public int GetIndex(string name)
    {
        if (crops == null) return -1;
        for (int i = 0; i < crops.Length; i++)
            if (crops[i].name == name) return i;
        return -1;
    }
}
