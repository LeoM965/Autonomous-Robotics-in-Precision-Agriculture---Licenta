using UnityEngine;
public static class CameraHelper
{
    public static Vector3 SmoothFollow(Vector3 current, Vector3 target, float speed, float deltaTime)
    {
        return Vector3.Lerp(current, target, deltaTime * speed);
    }
    public static Vector3 GetOrbitPosition(Vector3 center, float angleX, float angleY, float distance)
    {
        Quaternion rotation = Quaternion.Euler(angleX, angleY, 0);
        Vector3 offset = rotation * Vector3.back * distance;
        return center + offset;
    }
    public static Quaternion SmoothLookAt(Transform camera, Vector3 target, float speed, float deltaTime)
    {
        Vector3 direction = target - camera.position;
        if (direction.sqrMagnitude < 0.001f)
            return camera.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        return Quaternion.Slerp(camera.rotation, targetRotation, deltaTime * speed);
    }
    public static float ClampAngle(float angle)
    {
        return Mathf.Clamp(angle, -10f, 80f);
    }
    public static float GetNextZoom(float current, float[] presets, bool zoomIn)
    {
        int closest = FindClosestPreset(current, presets);
        if (zoomIn && closest > 0)
            return presets[closest - 1];
        if (!zoomIn && closest < presets.Length - 1)
            return presets[closest + 1];
        return current;
    }
    static int FindClosestPreset(float current, float[] presets)
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
