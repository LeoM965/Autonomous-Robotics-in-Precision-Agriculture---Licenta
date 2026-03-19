using UnityEngine;

namespace Robots.Movement.Interfaces
{
    public interface IRobotMovement
    {
        bool HasTarget { get; }
        bool HasArrived { get; }
        void SetTarget(Vector3 target);
        void Stop();
        void IgnoreCollisionWith(Collider target, bool ignore);
    }
}
