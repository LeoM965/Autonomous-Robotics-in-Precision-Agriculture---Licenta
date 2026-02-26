using UnityEngine;

public class CameraMotor : MonoBehaviour
{
    private Vector3 velocity;

    public Vector3 UpdateSmoothPosition(Vector3 current, Vector3 target, float speed, float dt)
    {
        return CameraHelper.SmoothFollow(current, target, speed * 2f, dt);
    }

    public void ApplyMovement(Transform camTransform, Vector3 desired, Vector3 center, CameraMode mode, float speed, float dt)
    {
        if (mode == CameraMode.FPS)
        {
            camTransform.position = Vector3.Lerp(camTransform.position, desired, dt * 15f);
        }
        else
        {
            camTransform.position = Vector3.SmoothDamp(camTransform.position, desired, ref velocity, 0.15f);
            camTransform.rotation = CameraHelper.SmoothLookAt(camTransform, center, speed * 1.5f, dt);
        }
    }
}
