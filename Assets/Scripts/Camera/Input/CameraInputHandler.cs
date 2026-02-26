using UnityEngine;

public class CameraInputHandler : MonoBehaviour
{
    public CameraSettings settings;
    
    public bool GetToggleModeDown() => Input.GetKeyDown(settings.toggleModeKey);
    public bool GetSwitchTargetDown() => Input.GetKeyDown(settings.switchTargetKey);
    public bool GetResetDown() => Input.GetKeyDown(settings.resetKey);
    
    public float GetZoomScroll() => Input.mouseScrollDelta.y;
    public bool GetOrbitPressed() => Input.GetMouseButton(1);
    
    public float GetMouseX() => Input.GetAxis("Mouse X");
    public float GetMouseY() => Input.GetAxis("Mouse Y");
}
