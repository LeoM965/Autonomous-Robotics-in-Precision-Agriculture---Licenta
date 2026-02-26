using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "Robotics/Camera Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Movement")]
    public float smoothSpeed = 5f;
    public float sensitivity = 3f;
    public float distance = 12f;
    
    [Header("Zoom")]
    public float[] zoomPresets = { 3f, 5f, 12.5f, 25f };
    
    [Header("Input Keys")]
    public KeyCode toggleModeKey = KeyCode.C;
    public KeyCode switchTargetKey = KeyCode.V;
    public KeyCode resetKey = KeyCode.R;
    
    [Header("Offsets")]
    public Vector3 upOffset = new Vector3(0, 3f, 0);
    public float fpsHeightOffset = 2.2f;
    public float fpsForwardOffset = 0.4f;
}
