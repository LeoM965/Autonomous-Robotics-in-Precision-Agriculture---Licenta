using UnityEngine;

[CreateAssetMenu(fileName = "MapColors", menuName = "Farm/Map Colors")]
public class MapColors : ScriptableObject
{
    [Header("Main Colors")]
    public Color backgroundColor;
    public Color headerBackgroundColor;
    public Color borderColor;
    public Color gridColor;
    public Color buildingColor;

    [Header("Zones")]
    public Color[] zoneColors;
    public Color agBotColor;
    public Color harvestBotColor;
    public Color agroBotColor;
    public Color defaultRobotColor;

    public Color GetZoneColor(int index)
    {
        if (zoneColors == null || zoneColors.Length == 0) return Color.gray;
        return zoneColors[index % zoneColors.Length];
    }

    public Color GetRobotColor(string robotName)
    {
        if (string.IsNullOrEmpty(robotName)) return defaultRobotColor;
        
        if (robotName.StartsWith("HarvestBot")) return harvestBotColor;
        if (robotName.StartsWith("AgroBot")) return agroBotColor;
        if (robotName.StartsWith("AgBot")) return agBotColor;
        
        return defaultRobotColor;
    }
}
