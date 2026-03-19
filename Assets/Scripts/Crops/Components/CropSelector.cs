using UnityEngine;
using System.Collections.Generic;
using Sensors.Models;
using AI.Models.Decisions;
using Settings;

public static class CropSelector
{
    public delegate void OnCropSelected(Transform robot, CropData crop, float score, List<DecisionAlternative> alternatives, SoilComposition soil, string parcelName);
    public static event OnCropSelected CropSelected;

    public static CropData SelectBestCrop(CropDatabase db, SoilComposition soil, Transform robot, string parcelName)
    {
        if (db == null || db.crops == null || db.crops.Length == 0)
            return null;

        // Ensure settings are synced with the database (especially if we added new crops)
        if (!SimulationSettings.IsInitialized || SimulationSettings.SeedCosts.Length != db.crops.Length)
        {
            SimulationSettings.InitFromDatabase(db);
        }
        
        CropData bestCrop = null;
        float bestScore = 0f;
        List<DecisionAlternative> alternatives = new List<DecisionAlternative>();

        for (int i = 0; i < db.crops.Length; i++)
        {
            CropData crop = db.crops[i];
            float suitability = 0f;
            float temperature = Weather.Components.WeatherSystem.Instance != null 
                ? Weather.Components.WeatherSystem.Instance.CurrentTemperature 
                : 20f;

            bool isWinter = false;
            if (TimeManager.Instance != null)
            {
                isWinter = TimeManager.Instance.GetCurrentSeason() == Weather.Models.Season.Winter;
            }
            else
            {
                isWinter = temperature < 5f;
            }

            // Exclusivitate sezonieră: plantele de iarnă (isFrostResistant) DOAR iarna. Restul DOAR în celelalte sezoane.
            bool canPlant = isWinter == crop.isFrostResistant;

            if (canPlant && crop.requirements != null)
            {
                // Use dynamic settings from the 'S' menu (including the 40% soft buffer)
                suitability = crop.requirements.CalculateTotalScore(soil, 
                    SimulationSettings.N_Min[i], SimulationSettings.N_Opt[i], SimulationSettings.N_Max[i],
                    SimulationSettings.P_Min[i], SimulationSettings.P_Opt[i], SimulationSettings.P_Max[i],
                    SimulationSettings.K_Min[i], SimulationSettings.K_Opt[i], SimulationSettings.K_Max[i]);
            }

            if (suitability > bestScore)
            {
                bestScore = suitability;
                bestCrop = crop;
            }

            alternatives.Add(new DecisionAlternative(crop.name, suitability));
        }

        MarkChosen(alternatives, bestCrop);
        // Sort by Suitability
        alternatives.Sort((a, b) => b.score.CompareTo(a.score));
        
        CropSelected?.Invoke(robot, bestCrop, bestScore, alternatives, soil, parcelName);
        return bestCrop;
    }

    private static void MarkChosen(List<DecisionAlternative> alternatives, CropData bestCrop)
    {
        if (bestCrop == null) return;
        for (int i = 0; i < alternatives.Count; i++)
        {
            if (alternatives[i].name == bestCrop.name)
            {
                alternatives[i] = new DecisionAlternative(alternatives[i].name, alternatives[i].score, true);
                break;
            }
        }
    }
}
