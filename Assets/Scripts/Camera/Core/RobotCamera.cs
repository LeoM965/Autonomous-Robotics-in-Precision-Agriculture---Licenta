using UnityEngine;
using System.Collections.Generic;

public class RobotCamera : MonoBehaviour
{
    [Header("Components")]
    public CameraSettings settings;
    public CameraInputHandler input;
    public CameraMotor motor;
    public CameraCollisionHandler collision;
    public CameraDisplay display;
    
    [Header("Targets")]
    public Transform target;
    public List<Transform> targets = new List<Transform>();

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    
    [Header("State")]
    [SerializeField] private CameraState state = new CameraState();
    [SerializeField] private bool showHUD = true;
    
    private CameraMode mode = CameraMode.Follow;
    private int targetIndex;

    private void Start() => Initialize();

    private void Initialize()
    {
        if (!input) input = gameObject.AddComponent<CameraInputHandler>();
        if (!motor) motor = gameObject.AddComponent<CameraMotor>();
        if (!collision) collision = gameObject.AddComponent<CameraCollisionHandler>();
        if (!display) display = gameObject.AddComponent<CameraDisplay>();
        
        if (settings == null)
        {
            Debug.LogError("<b>[CameraSystem]</b> CameraSettings NU este asignat! Click Dreapta în Project -> Create -> Robotics -> Camera Settings, apoi trage fișierul creat în Inspector peste RobotCamera.");
            return;
        }

        input.settings = settings;
        
        if (state == null) state = new CameraState();
        state.currentDistance = settings.distance;

        if (target == null && targets != null && targets.Count > 0) target = targets[0];
        if (target != null) state.smoothPosition = target.position + settings.upOffset;
    }

    private void FixedUpdate()
    {
        if (!target || !settings) return;
        
        float dt = Time.fixedDeltaTime;
        state.smoothPosition = motor.UpdateSmoothPosition(state.smoothPosition, target.position + settings.upOffset, settings.smoothSpeed, dt);
        state.smoothAngle = Mathf.LerpAngle(state.smoothAngle, target.eulerAngles.y, dt * 3f);
        
        UpdateDesiredPosition();
    }

    private void LateUpdate()
    {
        if (!target || !settings) return;
        
        ProcessInput();
        motor.ApplyMovement(transform, state.desiredPosition, state.smoothPosition, mode, settings.smoothSpeed, Time.deltaTime);
        if (mode == CameraMode.FPS) transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * 12f);
    }

    private void ProcessInput()
    {
        if (input.GetToggleModeDown()) mode = (CameraMode)(((int)mode + 1) % 4);
        
        if (input.GetSwitchTargetDown() && targets.Count > 1)
        {
            targetIndex = (targetIndex + 1) % targets.Count;
            target = targets[targetIndex];
        }

        if (input.GetResetDown()) ResetState();

        float scroll = input.GetZoomScroll();
        if (Mathf.Abs(scroll) > 0.1f)
            state.currentDistance = CameraHelper.GetNextZoom(state.currentDistance, settings.zoomPresets, scroll > 0);

        if (input.GetOrbitPressed() && mode != CameraMode.FPS)
        {
            state.angleY += input.GetMouseX() * settings.sensitivity;
            state.angleX = CameraHelper.ClampAngle(state.angleX - input.GetMouseY() * settings.sensitivity);
        }
    }

    private void ResetState()
    {
        state.angleX = 20f;
        state.angleY = 0f;
        state.currentDistance = settings.distance;
        mode = CameraMode.Follow;
    }

    private void UpdateDesiredPosition()
    {
        if (mode == CameraMode.Follow || mode == CameraMode.Orbital || mode == CameraMode.FPS || mode == CameraMode.TopView)
        {
            state.desiredPosition = CameraLogic.GetModePosition(mode, state.smoothPosition, state.angleX, state.angleY, state.currentDistance, target, settings);
            state.desiredPosition = collision.AdjustForCollision(state.desiredPosition, state.smoothPosition, collisionMask);
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
