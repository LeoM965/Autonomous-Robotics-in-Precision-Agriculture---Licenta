using UnityEngine;

namespace Robots.Components.Movement
{
    public class RobotMotor : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float speed = 5f;
        public float rotationSpeed = 120f;
        public float heightSpeed = 10f;
        public float tiltSpeed = 3f;
        public float maxTilt = 20f;

        [Header("Avoidance")]
        public float avoidRadius = 2f;

        // Surface adaptor state (inlined from RobotSurfaceAdaptor)
        private Terrain terrain;
        private Rect movementBounds;
        private float groundOffset;
        private float currentAngle;
        private float targetAngle;
        private Vector3 velocity;
        private bool isStopped;
        private RobotPathfinder pathfinder;

        private float currentHeight;
        private float currentPitch;
        private float currentRoll;
        private Vector3 groundNormal = Vector3.up;
        private float heightCheckTimer;
        private const float HEIGHT_CHECK_INTERVAL = 0.1f;

        public void Initialize(Terrain t, Rect bounds, float offset)
        {
            terrain = t;
            movementBounds = bounds;
            groundOffset = offset;
            currentAngle = targetAngle = transform.eulerAngles.y;
            pathfinder = GetComponent<RobotPathfinder>();

            if (terrain != null)
                currentHeight = TerrainHelper.GetHeight(transform.position) + groundOffset;
        }

        public void Stop()
        {
            velocity = Vector3.zero;
            isStopped = true;
        }

        public void Resume() => isStopped = false;

        private void FixedUpdate()
        {
            if (terrain == null || pathfinder == null) return;

            if (isStopped)
            {
                velocity = Vector3.Lerp(velocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                return;
            }

            float dt = Time.fixedDeltaTime;
            Vector3 pos = transform.position;

            float newTargetAngle;
            Vector3 moveDirection = pathfinder.GetMoveDirection(pos, dt, out newTargetAngle);
            targetAngle = newTargetAngle;

            Vector3 avoidanceDir = RobotHelper.GetObstacleAvoidance(transform, pos, avoidRadius);
            if (avoidanceDir != Vector3.zero)
                moveDirection = (moveDirection + avoidanceDir).normalized;

            Vector3 stuckPush = pathfinder.StuckPushDir;
            if (stuckPush != Vector3.zero)
                moveDirection = (moveDirection + stuckPush).normalized;

            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
            float speedMultiplier = angleDiff > 45f ? 0.3f : (angleDiff > 15f ? 0.7f : 1f);

            float weatherPenalty = 1.0f;
            if (Weather.Components.WeatherSystem.Instance != null)
                weatherPenalty = Weather.Components.WeatherSystem.Instance.GetMovementPenalty();

            velocity = Vector3.Lerp(velocity, moveDirection * speed * speedMultiplier * weatherPenalty, dt * 5f);
            pos += velocity * dt;
            pos = BoundsHelper.ClampPosition(pos, movementBounds);

            ApplySurface(ref pos, targetAngle, dt);
            currentAngle = transform.eulerAngles.y;
        }

        private void ApplySurface(ref Vector3 pos, float surfaceTargetAngle, float dt)
        {
            if (terrain == null) return;

            bool shouldCheck = velocity.sqrMagnitude > 0.25f;
            heightCheckTimer += dt;

            if (shouldCheck || heightCheckTimer >= HEIGHT_CHECK_INTERVAL)
            {
                heightCheckTimer = 0f;
                Vector3 normal;
                float targetH = RobotHelper.GetHeight(terrain, transform, pos, out normal) + groundOffset;

                float heightDiff = Mathf.Abs(targetH - currentHeight);
                currentHeight = heightDiff > 0.5f ? targetH : Mathf.Lerp(currentHeight, targetH, dt * heightSpeed);
                groundNormal = Vector3.Lerp(groundNormal, normal, dt * 5f);
            }

            pos.y = currentHeight;
            transform.position = pos;

            RobotHelper.UpdateTilt(ref currentAngle, surfaceTargetAngle, rotationSpeed, groundNormal,
                maxTilt, tiltSpeed, ref currentPitch, ref currentRoll, transform, dt);
        }

        public void SetMovementBounds(Rect bounds) => movementBounds = bounds;
        public void SetTerrain(Terrain t) => terrain = t;
        
        public void Randomize(float speedVar, float rotVar, float tiltVar, float maxTiltVar, float avoidVar)
        {
            speed *= speedVar;
            rotationSpeed *= rotVar;
            tiltSpeed *= tiltVar;
            maxTilt *= maxTiltVar;
            avoidRadius *= avoidVar;
        }
    }
}
