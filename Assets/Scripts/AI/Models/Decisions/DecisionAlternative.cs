namespace AI.Models.Decisions
{
    [System.Serializable]
    public class DecisionAlternative
    {
        public string name;
        public float score;
        public bool isChosen;
        
        public DecisionAlternative(string name, float score, bool isChosen = false)
        {
            this.name = name;
            this.score = score;
            this.isChosen = isChosen;
        }
    }
}
