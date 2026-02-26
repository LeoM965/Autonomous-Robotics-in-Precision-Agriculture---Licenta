using UnityEngine;

public class CameraCollisionHandler : MonoBehaviour
{
    public Vector3 AdjustForCollision(Vector3 desired, Vector3 center, LayerMask mask)
    {
        if (mask == 0) return desired;

        Vector3 dir = desired - center;
        if (Physics.Raycast(center, dir.normalized, out RaycastHit hit, dir.magnitude, mask))
        {
            return hit.point - dir.normalized * 0.2f;
        }
        return desired;
    }
}
