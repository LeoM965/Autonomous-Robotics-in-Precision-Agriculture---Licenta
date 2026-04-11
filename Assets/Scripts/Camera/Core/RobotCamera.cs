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
    private bool isPanning;
    private Vector3 panFocusPoint;
    private float panIdleTimer;
    private const float PAN_RETURN_DELAY = 1.0f;

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
        Vector3 targetPos = isPanning ? panFocusPoint : (target.position + settings.upOffset);
        state.smoothPosition = Vector3.Lerp(state.smoothPosition, targetPos, dt * settings.smoothSpeed * 2f);
        
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
            mode = (CameraMode)(((int)mode + 1) % 4);
        
        if (Input.GetKeyDown(settings.switchTargetKey) && targets.Count > 1)
        {
            targetIndex = (targetIndex + 1) % targets.Count;
            target = targets[targetIndex];
            isPanning = false;
        }

        if (Input.GetKeyDown(settings.resetKey))
        {
            state.angleX = 20f;
            state.angleY = 0f;
            state.currentDistance = settings.distance;
            mode = CameraMode.Follow;
            isPanning = false;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.1f)
            state.currentDistance = CameraHelper.GetNextZoom(state.currentDistance, settings.zoomPresets, scroll > 0);

        if (Input.GetMouseButton(1) && mode != CameraMode.FPS)
        {
            state.angleY += Input.GetAxis("Mouse X") * settings.sensitivity;
            state.angleX = CameraHelper.ClampAngle(state.angleX - Input.GetAxis("Mouse Y") * settings.sensitivity, settings.minAngleX, settings.maxAngleX);
        }

        // --- WASD Map Navigation ---
        Vector2 navInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        if (navInput.magnitude > 0.2f)
        {
            if (!isPanning)
            {
                isPanning = true;
                panFocusPoint = state.smoothPosition;
            }

            Quaternion rotation = Quaternion.Euler(0, state.angleY, 0);
            Vector3 camForward = rotation * Vector3.forward;
            Vector3 camRight = rotation * Vector3.right;

            Vector3 move = (camForward * navInput.y + camRight * navInput.x).normalized;
            panFocusPoint += move * settings.panSpeed * Time.deltaTime;
            panIdleTimer = 0f;
        }
        else if (isPanning)
        {
            // Revenire automata la follow dupa ce nu mai apesi WASD
            panIdleTimer += Time.deltaTime;
            if (panIdleTimer >= PAN_RETURN_DELAY)
            {
                isPanning = false;
                panIdleTimer = 0f;
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;
        target = newTarget;
        state.smoothPosition = target.position + settings.upOffset;
        isPanning = false;
    }

    private void OnGUI()
    {
        if (showHUD && target && display) display.DrawOverlay(mode, target.name, state.currentDistance);
    }
}
