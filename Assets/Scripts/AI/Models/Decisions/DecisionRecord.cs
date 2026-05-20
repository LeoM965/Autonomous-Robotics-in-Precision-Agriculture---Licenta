using System.Collections.Generic;
using AI.Models.Decisions;

namespace AI.Analytics
{
    [System.Serializable]
    public class DecisionRecord
    {
        public string decisionType;
        public string chosenOption;
        public float chosenScore;
        public float netValue;
        public string parcelName;
        public float timestamp;
        public float schedulingValue;
        public int globalIndex;
        public List<DecisionAlternative> alternatives;
        public DecisionFactors factors;

        // ── Fertilization details (populated only for Treat Soil) ──
        public string cropVariety;        // name of the planted crop
        public float initialN, initialP, initialK, initialPH, initialMoisture;   // nutrient/moisture levels BEFORE treatment
        public float appliedN, appliedP, appliedK, appliedPH, appliedMoisture;    // total amount applied during treatment
        public float optimalN, optimalP, optimalK, optimalPH, optimalMoisture;    // crop-specific optimal targets

        // ── ML Decision Tree prediction (populated only for Selectie Cultura) ──
        public string mlPrediction;   // crop variety recommended by the trained ML model
        
        public DecisionRecord()
        {
            alternatives = new List<DecisionAlternative>();
            factors = new DecisionFactors();
        }
    }
}
