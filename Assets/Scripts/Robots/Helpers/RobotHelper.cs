using UnityEngine;
using System.Collections.Generic;

public static class RobotHelper
{
    private static readonly RaycastHit[] rayHits = new RaycastHit[16];
    private static readonly Collider[] overlapResults = new Collider[16];

    public static float CalculateGroundOffset(Transform robot)
    {
        Renderer renderer = robot.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return robot.position.y - renderer.bounds.min.y;
        return 0.3f;
    }

    public static void UpdateWheelRotation(Transform[] wheels, Vector3[] originalAngles, float rotation)
    {
        if (wheels == null || originalAngles == null)
            return;
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
            {
                wheels[i].localRotation = Quaternion.Euler(
                    rotation,
                    originalAngles[i].y,
                    originalAngles[i].z
                );
            }
        }
    }

    public static Vector3 GetObstacleAvoidance(Transform transform, Vector3 pos, float avoidRadius)
    {
        RaycastHit hit;
        if (!Physics.Raycast(pos + Vector3.up * 0.5f, transform.forward, out hit, avoidRadius, ~0, QueryTriggerInteraction.Ignore))
            return Vector3.zero;
        
        if (hit.transform.IsChildOf(transform))
            return Vector3.zero;

        if (hit.transform.GetComponent<Sensors.Components.EnvironmentalSensor>() != null)
            return Vector3.zero;
        
        float strength = 1f - (hit.distance / avoidRadius);
        return Vector3.Cross(Vector3.up, transform.forward) * strength * 2f;
    }
    
    public static float GetHeight(Terrain terrain, Transform robotTransform, Vector3 pos, out Vector3 normal)
    {
        normal = Vector3.up;
        float terrainH = terrain != null ? terrain.SampleHeight(pos) + terrain.transform.position.y : 0f;
        float height = terrainH;
        
        int hitCount = Physics.RaycastNonAlloc(new Vector3(pos.x, height + 50f, pos.z), Vector3.down, rayHits, 100f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = rayHits[i];
            if (hit.transform.IsChildOf(robotTransform) || hit.point.y - terrainH > 6f)
                continue;
            if (hit.point.y > height)
            {
                height = hit.point.y;
                normal = hit.normal;
            }
        }
        
        int overlapCount = Physics.OverlapSphereNonAlloc(pos, 2f, overlapResults, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider col = overlapResults[i];
            if (col.transform.IsChildOf(robotTransform)) continue;
            if (col.GetComponent<Sensors.Components.EnvironmentalSensor>() == null && col.GetComponentInParent<Sensors.Components.EnvironmentalSensor>() == null) continue;
            Bounds b = col.bounds;
            float margin = 0.5f;
            if (pos.x >= b.min.x - margin && pos.x <= b.max.x + margin &&
                pos.z >= b.min.z - margin && pos.z <= b.max.z + margin)
            {
                float top = b.max.y;
                if (top > height && top - terrainH < 6f)
                    height = top;
            }
        }
        return height;
    }
    
    public static void UpdateTilt(
        ref float currentAngle,
        float targetAngle,
        float rotationSpeed,
        Vector3 groundNormal,
        float maxTilt,
        float tiltSpeed,
        ref float currentPitch,
        ref float currentRoll,
        Transform transform,
        float dt)
    {
        currentAngle += Mathf.Clamp(Mathf.DeltaAngle(currentAngle, targetAngle), -rotationSpeed * dt, rotationSpeed * dt);
        Quaternion yaw = Quaternion.Euler(0, currentAngle, 0);
        
        float pitchTarget = Mathf.Clamp(-Vector3.Dot(groundNormal, yaw * Vector3.forward) * 90f, -maxTilt, maxTilt);
        float rollTarget = Mathf.Clamp(Vector3.Dot(groundNormal, yaw * Vector3.right) * 90f, -maxTilt, maxTilt);
        
        currentPitch = Mathf.Lerp(currentPitch, pitchTarget, dt * tiltSpeed);
        currentRoll = Mathf.Lerp(currentRoll, rollTarget, dt * tiltSpeed);
        
        transform.rotation = Quaternion.Euler(currentPitch, currentAngle, currentRoll);
    }
    
    public static Vector3 UpdateStuckDetection(
        Vector3 pos,
        Vector3 lastFixedPos,
        List<Vector3> path,
        int pathIndex,
        ref float stuckTimer,
        ref Vector3 stuckPushDir,
        Vector3? finalTarget,
        System.Action<Vector3> requestPath,
        Transform transform,
        float dt)
    {
        float movedDist = (pos - lastFixedPos).magnitude;
        
        if (movedDist < 0.02f && path != null && pathIndex < path.Count)
        {
            stuckTimer += dt;
            if (stuckTimer > 0.5f)
            {
                if (stuckPushDir == Vector3.zero)
                    stuckPushDir = Random.value > 0.5f ? transform.right : -transform.right;
                
                if (stuckTimer > 2f && finalTarget.HasValue)
                {
                    requestPath(finalTarget.Value);
                    stuckTimer = 0f;
                    stuckPushDir = Vector3.zero;
                }
            }
        }
        else
        {
            stuckTimer = 0f;
            stuckPushDir = Vector3.zero;
        }
        
        return stuckPushDir;
    }
}
