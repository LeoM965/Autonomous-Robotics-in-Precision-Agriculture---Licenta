using UnityEngine;

namespace AI.Core
{
    public class RobotTask
    {
        public Transform Target { get; }
        public TaskType Type { get; }
        public float Priority { get; }

        public RobotTask(Transform target, TaskType type = TaskType.Scout, float priority = 0)
        {
            Target = target;
            Type = type;
            Priority = priority;
        }

        public override bool Equals(object obj) => obj is RobotTask other && Target == other.Target;
        public override int GetHashCode() => Target != null ? Target.GetHashCode() : 0;
    }
}
