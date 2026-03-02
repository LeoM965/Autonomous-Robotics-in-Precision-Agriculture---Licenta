using UnityEngine;
using System.Collections.Generic;

public class RobotCamera : MonoBehaviour
{
    [Header("Configuration")]
    public CameraSettings settings;
    
    [Header("Targets")]
    public Transform target;
    public List<Transform> targets = new List<Transform>();

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    
    [Header("Debug")]
    [SerializeField] private CameraState state = new CameraState();
    [SerializeField] private bool showHUD = true;
    
    private CameraMode mode = CameraMode.Follow;
    private int targetIndex;
    private CameraDisplay display;

    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("<b>[CameraSystem]</b> CameraSettings NU este asignat!");
            return;
        }
        
        display = gameObject.AddComponent<CameraDisplay>();
        state.currentDistance = settings.distance;

        if (target == null && targets.Count > 0) target = targets[0];
        if (target != null) state.smoothPosition = target.position + settings.upOffset;
    }

    private void FixedUpdate()
    {
        if (!target || !settings) return;
        
        float dt = Time.fixedDeltaTime;
        state.smoothPosition = Vector3.Lerp(state.smoothPosition, target.position + settings.upOffset, dt * settings.smoothSpeed * 2f);
        
        state.desiredPosition = CameraHelper.GetModePosition(mode, state.smoothPosition, state.angleX, state.angleY, state.currentDistance, target, settings);
        state.desiredPosition = CameraHelper.AdjustForCollision(state.desiredPosition, state.smoothPosition, collisionMask);
    }

    private void LateUpdate()
    {
        if (!target || !settings) return;
        
        ProcessInput();
        
        if (mode == CameraMode.FPS)
        {
            transform.position = Vector3.Lerp(transform.position, state.desiredPosition, Time.deltaTime * settings.fpsPositionSmooth);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * settings.fpsRotationSmooth);
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, state.desiredPosition, ref state.velocity, settings.smoothDampTime);
            transform.rotation = CameraHelper.SmoothLookAt(transform, state.smoothPosition, settings.smoothSpeed * 1.5f, Time.deltaTime);
        }
    }

    private void ProcessInput()
    {
        if (Input.GetKeyDown(settings.toggleModeKey))
            mode = (CameraMode)(((int)mode + 1) % 4);
        
        if (Input.GetKeyDown(settings.switchTargetKey) && targets.Count > 1)
        {
            targetIndex = (targetIndex + 1) % targets.Count;
            target = targets[targetIndex];
        }

        if (Input.GetKeyDown(settings.resetKey))
        {
            state.angleX = 20f;
            state.angleY = 0f;
            state.currentDistance = settings.distance;
            mode = CameraMode.Follow;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.1f)
            state.currentDistance = CameraHelper.GetNextZoom(state.currentDistance, settings.zoomPresets, scroll > 0);

        if (Input.GetMouseButton(1) && mode != CameraMode.FPS)
        {
            state.angleY += Input.GetAxis("Mouse X") * settings.sensitivity;
            state.angleX = CameraHelper.ClampAngle(state.angleX - Input.GetAxis("Mouse Y") * settings.sensitivity, settings.minAngleX, settings.maxAngleX);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;
        target = newTarget;
        state.smoothPosition = target.position + settings.upOffset;
    }

    private void OnGUI()
    {
        if (showHUD && target && display) display.DrawOverlay(mode, target.name, state.currentDistance);
    }
}
