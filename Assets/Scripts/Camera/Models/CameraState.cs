using UnityEngine;

[System.Serializable]
public class CameraState
{
    public Vector3 smoothPosition;
    public Vector3 desiredPosition;
    public Vector3 velocity;
    public float currentDistance;
    public float angleX = 20f;
    public float angleY;
}
