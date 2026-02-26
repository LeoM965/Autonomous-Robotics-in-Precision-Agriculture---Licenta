using UnityEngine;

[System.Serializable]
public class CameraState
{
    public Vector3 smoothPosition;
    public Vector3 desiredPosition;
    public float currentDistance;
    public float angleX = 20f;
    public float angleY;
    public float smoothAngle;
    public int targetIndex;
}
