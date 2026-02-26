using UnityEngine;
using System.Collections.Generic;
using Sensors.Models;

public static class CropSelector
{
    public static CropData SelectBestCropWithTracking(CropDatabase db, SoilComposition soil, Transform robot, string parcelName)
    {
        if (db == null || db.crops == null || db.crops.Length == 0)
            return null;
        
        CropData bestCrop = null;
        float bestScore = -1f;
        List<DecisionAlternative> alternatives = new List<DecisionAlternative>();
        
        for (int i = 0; i < db.crops.Length; i++)
        {
            float score = CalculateSuitability(db.crops[i], soil);
            alternatives.Add(new DecisionAlternative(db.crops[i].name, score));
            
            if (score > bestScore)
            {
                bestScore = score;
                bestCrop = db.crops[i];
            }
        }
        
        // Mark the chosen one
        for (int i = 0; i < alternatives.Count; i++)
        {
            if (bestCrop != null && alternatives[i].name == bestCrop.name)
            {
                alternatives[i] = new DecisionAlternative(alternatives[i].name, alternatives[i].score, true);
                break;
            }
        }
        
        // Sort by score descending
        alternatives.Sort((a, b) => b.score.CompareTo(a.score));
        
        // Create decision record with factors
        DecisionRecord record = new DecisionRecord
        {
            decisionType = "Selectie Cultura",
            chosenOption = bestCrop != null ? bestCrop.name : "Niciuna",
            chosenScore = bestScore,
            alternatives = alternatives,
            factors = CalculateFactors(bestCrop, soil),
            parcelName = parcelName
        };

        if (DecisionTracker.Instance != null)
            DecisionTracker.Instance.RecordDecision(robot, record);
        
        return bestCrop;
    }

    private static DecisionFactors CalculateFactors(CropData crop, SoilComposition soil)
    {
        DecisionFactors factors = new DecisionFactors();
        
        if (crop == null || crop.requirements == null)
            return factors;
        
        factors.phScore = CalculateParameterScore(soil.pH, crop.requirements.pH);
        factors.humidityScore = CalculateParameterScore(soil.moisture, crop.requirements.humidity);
        factors.nitrogenScore = CalculateParameterScore(soil.nitrogen, crop.requirements.nitrogen);
        factors.phosphorusScore = CalculateParameterScore(soil.phosphorus, crop.requirements.phosphorus);
        factors.potassiumScore = CalculateParameterScore(soil.potassium, crop.requirements.potassium);
        
        return factors;
    }
    
    private static float CalculateSuitability(CropData crop, SoilComposition soil)
    {
        if (crop.requirements == null)
            return 0f;

        float total = 0f;
        int count = 0;

        total += CalculateParameterScore(soil.pH, crop.requirements.pH);
        count++;
        total += CalculateParameterScore(soil.moisture, crop.requirements.humidity);
        count++;
        total += CalculateParameterScore(soil.nitrogen, crop.requirements.nitrogen);
        count++;

        if (crop.requirements.phosphorus != null)
        {
            total += CalculateParameterScore(soil.phosphorus, crop.requirements.phosphorus);
            count++;
        }
        if (crop.requirements.potassium != null)
        {
            total += CalculateParameterScore(soil.potassium, crop.requirements.potassium);
            count++;
        }

        return count > 0 ? total / count : 0f;
    }
    
    private static float CalculateParameterScore(float value, CropRange range)
    {
        if (range == null)
            return 0f;
        if (value < range.min || value > range.max)
            return 0f;
        float distanceFromOptimal = Mathf.Abs(value - range.optimal);
        float maxDistance = Mathf.Max(range.optimal - range.min, range.max - range.optimal);
        if (maxDistance <= 0)
            return 100f;
        return 100f * (1f - distanceFromOptimal / maxDistance);
    }
}
