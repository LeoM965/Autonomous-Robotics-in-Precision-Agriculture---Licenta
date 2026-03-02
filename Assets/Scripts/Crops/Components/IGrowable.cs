public interface IGrowable
{
    void Init(float time);
    void ManualUpdate(float deltaTime);
    void Harvest();
    bool IsFullyGrown { get; }
    bool IsBeingHarvested { get; }
    float Progress { get; }
}
