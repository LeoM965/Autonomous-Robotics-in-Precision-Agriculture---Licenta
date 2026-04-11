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
        
        public DecisionRecord()
        {
            alternatives = new List<DecisionAlternative>();
            factors = new DecisionFactors();
        }
    }
}
