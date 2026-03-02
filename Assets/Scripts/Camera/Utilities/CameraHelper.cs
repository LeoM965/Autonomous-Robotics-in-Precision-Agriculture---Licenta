using UnityEngine;

public static class CameraHelper
{
    public static Vector3 GetOrbitPosition(Vector3 center, float angleX, float angleY, float distance)
    {
        Quaternion rotation = Quaternion.Euler(angleX, angleY, 0);
        return center + rotation * Vector3.back * distance;
    }

    public static Vector3 GetModePosition(CameraMode mode, Vector3 smoothPos, float angleX, float angleY, float distance, Transform target, CameraSettings settings)
    {
        return mode switch
        {
            CameraMode.Follow => GetOrbitPosition(smoothPos, angleX, target.eulerAngles.y, distance),
            CameraMode.Orbital => GetOrbitPosition(smoothPos, angleX, angleY, distance),
            CameraMode.TopView => smoothPos + Vector3.up * (distance - 3f),
            CameraMode.FPS => target.position + target.up * settings.fpsHeightOffset + target.forward * settings.fpsForwardOffset,
            _ => smoothPos
        };
    }

    public static Quaternion SmoothLookAt(Transform camera, Vector3 target, float speed, float deltaTime)
    {
        Vector3 direction = target - camera.position;
        if (direction.sqrMagnitude < 0.001f)
            return camera.rotation;
        return Quaternion.Slerp(camera.rotation, Quaternion.LookRotation(direction), deltaTime * speed);
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        return Mathf.Clamp(angle, min, max);
    }

    public static float GetNextZoom(float current, float[] presets, bool zoomIn)
    {
        int closest = FindClosestPreset(current, presets);
        if (zoomIn && closest > 0) return presets[closest - 1];
        if (!zoomIn && closest < presets.Length - 1) return presets[closest + 1];
        return current;
    }

    public static Vector3 AdjustForCollision(Vector3 desired, Vector3 center, LayerMask mask)
    {
        if (mask == 0) return desired;
        Vector3 dir = desired - center;
        if (Physics.Raycast(center, dir.normalized, out RaycastHit hit, dir.magnitude, mask))
            return hit.point - dir.normalized * 0.2f;
        return desired;
    }

    private static int FindClosestPreset(float current, float[] presets)
    {
        int closest = 0;
        float minDiff = Mathf.Abs(current - presets[0]);
        for (int i = 1; i < presets.Length; i++)
        {
            float diff = Mathf.Abs(current - presets[i]);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = i;
            }
        }
        return closest;
    }
}
