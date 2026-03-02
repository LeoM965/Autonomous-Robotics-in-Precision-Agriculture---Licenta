using UnityEngine;
using System.Collections.Generic;
using Sensors.Models;
using AI.Models.Decisions;

public static class CropSelector
{
    public delegate void OnCropSelected(Transform robot, CropData crop, float score, List<DecisionAlternative> alternatives, SoilComposition soil, string parcelName);
    public static event OnCropSelected CropSelected;

    public static CropData SelectBestCrop(CropDatabase db, SoilComposition soil, Transform robot, string parcelName)
    {
        if (db == null || db.crops == null || db.crops.Length == 0)
            return null;
        
        CropData bestCrop = null;
        float bestScore = -1f;
        List<DecisionAlternative> alternatives = new List<DecisionAlternative>();
        
        for (int i = 0; i < db.crops.Length; i++)
        {
            CropData crop = db.crops[i];
            float score = crop.requirements != null ? crop.requirements.CalculateTotalScore(soil) : 0f;
            alternatives.Add(new DecisionAlternative(crop.name, score));
            
            if (score > bestScore)
            {
                bestScore = score;
                bestCrop = crop;
            }
        }

        MarkChosen(alternatives, bestCrop);
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
