using UnityEngine;
using UnityEditor;
using System.IO;
using Weather.Models;

public class SeasonAssetGenerator
{
    [MenuItem("Robotics/Generate Climate Profiles")]
    public static void GenerateSeasons()
    {
        string path = "Assets/Resources/Seasons";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        CreateSeason(path, "Spring", Season.Spring, 5, 20, 0.5f, 0.3f, 0.05f, 0.05f, 0.1f, 1.0f, 3f);
        CreateSeason(path, "Summer", Season.Summer, 25, 35, 0.7f, 0.1f, 0.2f, 0.0f, 0.0f, 1.0f, 8f);
        CreateSeason(path, "Autumn", Season.Autumn, 5, 15, 0.3f, 0.4f, 0.1f, 0.0f, 0.2f, 0.7f, 2f); 
        CreateSeason(path, "Winter", Season.Winter, -10, 5, 0.4f, 0.0f, 0.0f, 0.4f, 0.2f, 0.5f, 1f); 

        AssetDatabase.Refresh();
        Debug.Log("Success! Climate Profiles Generated in " + path);
    }

    private static void CreateSeason(string folder, string name, Season type, float minT, float maxT, 
        float sunny, float rain, float storm, float snow, float fog, float moveMult, float evap)
    {
        ClimateProfile asset = ScriptableObject.CreateInstance<ClimateProfile>();
        asset.seasonName = name;
        asset.seasonType = type;
        asset.minTemp = minT;
        asset.maxTemp = maxT;
        
        asset.sunnyChance = sunny;
        asset.rainyChance = rain;
        asset.stormyChance = storm;
        asset.snowyChance = snow;
        asset.foggyChance = fog;
        
        asset.movementSpeedMultiplier = moveMult;
        asset.evaporationRate = evap;

        string fullPath = Path.Combine(folder, "Climate_" + name + ".asset");
        AssetDatabase.CreateAsset(asset, fullPath);
    }
}
