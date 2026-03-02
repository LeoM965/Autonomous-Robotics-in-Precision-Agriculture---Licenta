using UnityEngine;
using System.Collections.Generic;
using AI.Navigation;

public partial class RobotMovement
{
    Vector3 FollowPath(Vector3 pos, float dt)
    {
        stuckPushDir = RobotHelper.UpdateStuckDetection(
            pos, lastFixedPos, path, pathIndex,
            ref stuckTimer, ref stuckPushDir,
            finalTarget, RequestPath, transform, dt);

        lastFixedPos = pos;

        if (path == null || pathIndex >= path.Count)
            return Vector3.zero;

        Vector3 target = path[pathIndex];
        Vector3 dir = target - pos;
        dir.y = 0;
        float dist = dir.magnitude;

        if (dist < WAYPOINT_THRESHOLD)
        {
            pathIndex++;
            if (pathIndex >= path.Count && finalTarget.HasValue)
            {
                Vector3 toFinal = finalTarget.Value - pos;
                toFinal.y = 0;
                if (toFinal.magnitude > ARRIVAL_THRESHOLD)
                    RequestPath(finalTarget.Value);
            }
        }

        if (pathIndex < path.Count)
        {
            target = path[pathIndex];
            dir = target - pos;
            dir.y = 0;
            dist = dir.magnitude;
        }

        if (dist > ARRIVAL_THRESHOLD)
        {
            Vector3 moveDirection = dir.normalized;
            targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            return moveDirection;
        }

        return Vector3.zero;
    }

    void RequestPath(Vector3 target)
    {
        if (Pathfinder.Instance != null)
        {
            List<Vector3> newPath = Pathfinder.Instance.FindPath(transform.position, target);
            if (newPath != null && newPath.Count > 0)
            {
                path = newPath;
                pathIndex = 0;
                return;
            }
        }
        path = new List<Vector3> { target };
        pathIndex = 0;
    }
}
