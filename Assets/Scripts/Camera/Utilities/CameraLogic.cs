using UnityEngine;

public static class CameraLogic
{
    public static Vector3 GetModePosition(CameraMode mode, Vector3 smoothPos, float angleX, float angleY, float distance, Transform target, CameraSettings settings)
    {
        return mode switch
        {
            CameraMode.Follow => CameraHelper.GetOrbitPosition(smoothPos, angleX, target.eulerAngles.y, distance),
            CameraMode.Orbital => CameraHelper.GetOrbitPosition(smoothPos, angleX, angleY, distance),
            CameraMode.TopView => smoothPos + Vector3.up * (distance - 3f),
            CameraMode.FPS => target.position + target.up * settings.fpsHeightOffset + target.forward * settings.fpsForwardOffset,
            _ => smoothPos
        };
    }
}
