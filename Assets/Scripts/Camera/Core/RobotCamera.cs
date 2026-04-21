using UnityEngine;
using UnityEngine.EventSystems;
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

    // Free-pan state (WASD independent of robot)
    private bool isFreePan;
    private Vector3 freePanPosition;

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

    private Vector3 GetFocusPoint()
    {
        if (isFreePan)
            return freePanPosition;
        return target != null ? target.position + settings.upOffset : state.smoothPosition;
    }

    private void FixedUpdate()
    {
        if (settings == null) return;
        if (!isFreePan && target == null) return;
        
        float dt = Time.fixedDeltaTime;
        Vector3 focusPoint = GetFocusPoint();
        state.smoothPosition = Vector3.Lerp(state.smoothPosition, focusPoint, dt * settings.smoothSpeed * 2f);
        
        if (!isFreePan && target != null)
        {
            // Locked on robot: use distinct behaviors for each mode
            state.desiredPosition = CameraHelper.GetModePosition(mode, state.smoothPosition, state.angleX, state.angleY, state.currentDistance, target, settings);
        }
        else
        {
            // Free-pan mode: FPS and Follow act like Orbital (free orbit around WASD point), TopView looks straight down
            if (mode == CameraMode.TopView)
                state.desiredPosition = state.smoothPosition + Vector3.up * (state.currentDistance - 3f);
            else
                state.desiredPosition = CameraHelper.GetOrbitPosition(state.smoothPosition, state.angleX, state.angleY, state.currentDistance);
        }

        state.desiredPosition = CameraHelper.AdjustForCollision(state.desiredPosition, state.smoothPosition, collisionMask);
    }

    private void LateUpdate()
    {
        if (settings == null) return;
        if (!isFreePan && target == null) return;
        
        ProcessInput();
        
        if (mode == CameraMode.FPS && target != null && !isFreePan)
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

    private bool IsUIFocused()
    {
        if (EventSystem.current == null) return false;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;
        return selected.GetComponent<TMPro.TMP_InputField>() != null 
            || selected.GetComponent<UnityEngine.UI.InputField>() != null;
    }

    private void ProcessInput()
    {
        // Blocam input-ul camerei cand un input field UI e activ
        if (IsUIFocused()) return;

        if (Input.GetKeyDown(settings.toggleModeKey))
        {
            mode = (CameraMode)(((int)mode + 1) % 4);
            // Exiting FPS in free-pan uses orbital instead
        }
        
        if (Input.GetKeyDown(settings.switchTargetKey) && targets.Count > 1)
        {
            targetIndex = (targetIndex + 1) % targets.Count;
            target = targets[targetIndex];
            ExitFreePan();
        }

        if (Input.GetKeyDown(settings.resetKey))
        {
            state.angleX = 20f;
            state.angleY = 0f;
            state.currentDistance = settings.distance;
            mode = CameraMode.Follow;
            ExitFreePan();
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.1f)
            state.currentDistance = CameraHelper.GetNextZoom(state.currentDistance, settings.zoomPresets, scroll > 0);

        if (Input.GetMouseButton(1))
        {
            state.angleY += Input.GetAxis("Mouse X") * settings.sensitivity;
            state.angleX = CameraHelper.ClampAngle(state.angleX - Input.GetAxis("Mouse Y") * settings.sensitivity, settings.minAngleX, settings.maxAngleX);
        }

        // --- WASD Free Navigation (independent of robot) ---
        Vector2 navInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        if (navInput.magnitude > 0.2f)
        {
            if (!isFreePan) EnterFreePan();

            Quaternion rotation = Quaternion.Euler(0, state.angleY, 0);
            Vector3 move = (rotation * Vector3.forward * navInput.y + rotation * Vector3.right * navInput.x).normalized;
            freePanPosition += move * settings.panSpeed * Time.deltaTime;
        }
    }

    private void EnterFreePan()
    {
        isFreePan = true;
        freePanPosition = state.smoothPosition;
    }

    private void ExitFreePan()
    {
        isFreePan = false;
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;
        target = newTarget;
        state.smoothPosition = target.position + settings.upOffset;
        ExitFreePan();
    }

    private void OnGUI()
    {
        if (showHUD && display)
        {
            string label = isFreePan ? "Free Camera" : (target != null ? target.name : "No Target");
            display.DrawOverlay(mode, label, state.currentDistance);
        }
    }
}
