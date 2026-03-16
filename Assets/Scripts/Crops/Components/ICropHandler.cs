public interface ICropHandler
{
    void Initialize(float growthTime, float seedCost, float nConsumption = -1f, float nOptimal = -1f, int index = -1);
    void ManualUpdate(float currentTotalHours);
    void Harvest();
    bool IsFullyGrown { get; }
    bool IsBeingHarvested { get; }
    float Progress { get; }
}
