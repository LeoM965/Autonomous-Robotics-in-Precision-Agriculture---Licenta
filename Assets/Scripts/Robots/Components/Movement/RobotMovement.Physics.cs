using UnityEngine;

public partial class RobotMovement
{
    void FixedUpdate()
    {
        if (terrain == null) return;

        if (isStopped)
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.fixedDeltaTime * 5f);
            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector3 pos = transform.position;

        Vector3 moveDirection = FollowPath(pos, dt);

        Vector3 avoidance = MovementHelper.GetObstacleAvoidance(transform, pos, avoidRadius);
        if (avoidance != Vector3.zero)
            moveDirection = (moveDirection + avoidance).normalized;

        if (stuckPushDir != Vector3.zero)
            moveDirection = (moveDirection + stuckPushDir).normalized;

        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
        float speedMultiplier = angleDiff > 45f ? 0.3f : (angleDiff > 15f ? 0.7f : 1f);

        float weatherPenalty = 1.0f;
        if (Weather.Components.WeatherSystem.Instance != null)
        {
             weatherPenalty = Weather.Components.WeatherSystem.Instance.GetMovementPenalty();
        }

        velocity = Vector3.Lerp(velocity, moveDirection * speed * speedMultiplier * weatherPenalty, dt * 5f);
        pos += velocity * dt;

        float distMoved = Vector3.Distance(pos, lastFixedPos);
        if (distMoved > 0.001f && TimeManager.Instance != null)
        {
            TimeManager.Instance.AddDistanceTraveled(distMoved);
        }

        pos = BoundsHelper.ClampPosition(pos, movementBounds);

        Vector3 normal;
        float targetH = MovementHelper.GetHeight(terrain, transform, pos, out normal) + groundOffset;

        float heightDiff = Mathf.Abs(targetH - currentHeight);
        if (heightDiff > 0.5f)
            currentHeight = targetH;
        else
            currentHeight = Mathf.Lerp(currentHeight, targetH, dt * heightSpeed);
        pos.y = currentHeight;
        groundNormal = Vector3.Lerp(groundNormal, normal, dt * 5f);
        transform.position = pos;

        MovementHelper.UpdateTilt(ref currentAngle, targetAngle, rotationSpeed, groundNormal,
            maxTilt, tiltSpeed, ref currentPitch, ref currentRoll, transform, dt);
    }
}
