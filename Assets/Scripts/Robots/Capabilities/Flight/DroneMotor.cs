using UnityEngine;
using Robots.Models;

namespace Robots.Capabilities.Flight
{
    public class DroneMotor : MonoBehaviour
    {
        private const float MAX_SUBSTEP = 0.02f; // seconds per physics-like substep

        private Transform flightBody;
        private FlightSettings settings;
        private OperationRegion region;

        public void Initialize(Transform body, FlightSettings settings, OperationRegion region)
        {
            this.flightBody = body;
            this.settings = settings;
            this.region = region;
        }

        public void UpdateMovement(Vector3 target, bool isMoving)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Split large delta into small sub-steps for smooth movement at high timeScale
            int steps = Mathf.CeilToInt(dt / MAX_SUBSTEP);
            float stepDt = dt / steps;

            for (int i = 0; i < steps; i++)
            {
                if (isMoving)
                {
                    Vector3 dir = target - flightBody.position;
                    dir.y = 0;
                    if (dir.magnitude > 0.1f)
                    {
                        Vector3 moveDir = dir.normalized;
                        flightBody.position += moveDir * settings.speed * stepDt;
                        flightBody.rotation = Quaternion.Slerp(
                            flightBody.rotation,
                            Quaternion.LookRotation(moveDir),
                            stepDt * 3f);
                    }
                }
            }
            ApplyHoverAndClamping();
        }

        private void ApplyHoverAndClamping()
        {
            Vector3 pos = flightBody.position;
            pos.y = settings.altitude + Mathf.Sin(Time.time * settings.AngularHoverFrequency) * settings.hoverAmplitude;
            if (region != null)
            {
                pos.x = Mathf.Clamp(pos.x, region.Bounds.xMin - 5f, region.Bounds.xMax + 5f);
                pos.z = Mathf.Clamp(pos.z, region.Bounds.yMin - 5f, region.Bounds.yMax + 5f);
            }
            flightBody.position = pos;
        }

        public bool HasReached(Vector3 target)
        {
            Vector3 diff = target - flightBody.position;
            diff.y = 0;
            return diff.sqrMagnitude < settings.ArrivalThresholdSqr;
        }
    }
}
