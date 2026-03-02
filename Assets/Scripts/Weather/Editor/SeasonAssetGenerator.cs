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

        CreateSeason(path, "Spring", Season.Spring, 5, 20, 0.5f, 0.3f, 0.05f, 0.05f, 0.1f, 1.0f, 3f, 0.7f);
        CreateSeason(path, "Summer", Season.Summer, 25, 35, 0.7f, 0.1f, 0.2f, 0.0f, 0.0f, 1.0f, 8f, 0.8f);
        CreateSeason(path, "Autumn", Season.Autumn, 5, 15, 0.3f, 0.4f, 0.1f, 0.0f, 0.2f, 0.7f, 2f, 0.6f); 
        CreateSeason(path, "Winter", Season.Winter, -10, 5, 0.4f, 0.0f, 0.0f, 0.4f, 0.2f, 0.5f, 1f, 0.75f); 

        AssetDatabase.Refresh();
        Debug.Log("Success! Climate Profiles Generated in " + path);
    }

    private static void CreateSeason(string folder, string name, Season type, float minT, float maxT, 
        float sunny, float rain, float storm, float snow, float fog, float moveMult, float evap, float persistence)
    {
        ClimateProfile asset = ScriptableObject.CreateInstance<ClimateProfile>();
        asset.seasonName = name;
        asset.seasonType = type;
        asset.minTemp = minT;
        asset.maxTemp = maxT;
        
        asset.sunnyWeight = sunny;
        asset.rainyWeight = rain;
        asset.stormyWeight = storm;
        asset.snowyWeight = snow;
        asset.foggyWeight = fog;
        
        asset.movementMultiplier = moveMult;
        asset.evaporationRate = evap;
        asset.persistenceFactor = persistence;

        string fullPath = Path.Combine(folder, "Climate_" + name + ".asset");
        AssetDatabase.CreateAsset(asset, fullPath);
    }
}
