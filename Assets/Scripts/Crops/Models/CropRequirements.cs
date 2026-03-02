using System;
using System.Collections.Generic;
using UnityEngine;
using Sensors.Models;

[Serializable]
public class CropRequirements
{
    public CropRange pH;
    public CropRange humidity;
    public CropRange nitrogen;
    public CropRange phosphorus;
    public CropRange potassium;

    public IEnumerable<KeyValuePair<string, CropRange>> GetRanges()
    {
        if (pH != null) yield return new KeyValuePair<string, CropRange>("pH", pH);
        if (humidity != null) yield return new KeyValuePair<string, CropRange>("humidity", humidity);
        if (nitrogen != null) yield return new KeyValuePair<string, CropRange>("nitrogen", nitrogen);
        if (phosphorus != null) yield return new KeyValuePair<string, CropRange>("phosphorus", phosphorus);
        if (potassium != null) yield return new KeyValuePair<string, CropRange>("potassium", potassium);
    }

    public float CalculateTotalScore(SoilComposition soil)
    {
        float total = 0f;
        int count = 0;
        foreach (var pair in GetRanges())
        {
            total += pair.Value.GetScore(GetSoilValue(soil, pair.Key)) * 100f;
            count++;
        }
        return count > 0 ? total / count : 0f;
    }

    public AI.Models.Decisions.DecisionFactors BuildFactors(SoilComposition soil)
    {
        var factors = new AI.Models.Decisions.DecisionFactors();
        foreach (var pair in GetRanges())
        {
            float s = pair.Value.GetScore(GetSoilValue(soil, pair.Key)) * 100f;
            switch (pair.Key)
            {
                case "pH": factors.phScore = s; break;
                case "humidity": factors.humidityScore = s; break;
                case "nitrogen": factors.nitrogenScore = s; break;
                case "phosphorus": factors.phosphorusScore = s; break;
                case "potassium": factors.potassiumScore = s; break;
            }
        }
        return factors;
    }

    private static float GetSoilValue(SoilComposition soil, string key)
    {
        return key switch
        {
            "pH" => soil.pH,
            "humidity" => soil.moisture,
            "nitrogen" => soil.nitrogen,
            "phosphorus" => soil.phosphorus,
            "potassium" => soil.potassium,
            _ => 0f
        };
    }
}
