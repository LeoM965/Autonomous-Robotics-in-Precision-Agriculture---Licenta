using UnityEngine;
public static class MapColors
{
    public static readonly Color Background = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    public static readonly Color HeaderBackground = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    public static readonly Color Border = new Color(0.8f, 0.8f, 0.8f, 1f);
    public static readonly Color Grid = new Color(1f, 1f, 1f, 0.1f);
    public static readonly Color Building = new Color(1f, 0.8f, 0.2f, 1f);
    private static readonly Color[] ZoneColors = new Color[]
    {
        new Color(0.2f, 0.8f, 0.2f, 0.3f),
        new Color(0.8f, 0.2f, 0.2f, 0.3f),
        new Color(0.2f, 0.2f, 0.8f, 0.3f),
        new Color(0.8f, 0.8f, 0.2f, 0.3f)
    };
    public static Color GetZoneColor(int index)
    {
        return ZoneColors[index % ZoneColors.Length];
    }
    public static Color GetRobotColor(string name)
    {
        if (name.Contains("AgBot")) return new Color(0f, 1f, 0.2f); // Bright Green
        if (name.Contains("HarvestBot")) return new Color(1f, 0.5f, 0f); // Orange
        if (name.Contains("AgroBot")) return new Color(1f, 0.2f, 0.2f); // Red
        return Color.cyan;
    }
}
