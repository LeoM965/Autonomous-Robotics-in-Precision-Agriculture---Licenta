using System.Collections.Generic;

[System.Serializable]
public class DecisionRecord
{
    public string decisionType;
    public string chosenOption;
    public float chosenScore;
    public string parcelName;
    public float timestamp;
    public List<DecisionAlternative> alternatives;
    public DecisionFactors factors;
    
    public DecisionRecord()
    {
        alternatives = new List<DecisionAlternative>();
        factors = new DecisionFactors();
    }
}
