using UnityEngine;
public class RobotTask
{
    public Transform Target { get; private set; }
    public TaskType Type { get; private set; }
    public float Priority { get; private set; }
    public RobotTask(Transform target, TaskType type = TaskType.Scout, float priority = 0)
    {
        Target = target;
        Type = type;
        Priority = priority;
    }
    public override bool Equals(object obj)
    {
        RobotTask other = obj as RobotTask;
        if (other == null) return false;
        return Target == other.Target;
    }
    public override int GetHashCode()
    {
        if (Target == null) return 0;
        return Target.GetHashCode();
    }
}
