using UnityEngine;
using UnityEngine.EventSystems;

public class BatteryBarUI : MonoBehaviour
{
    [SerializeField] private RobotEnergy energy;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private Vector2 barSize = new Vector2(100f, 15f);
    
    private Camera mainCam;
    private bool isVisible;
    private float visCheckTimer;
    private GUIStyle chargingStyle;
    private const float VIS_CHECK_INTERVAL = 0.25f;
    private const float MAX_RENDER_DIST_SQR = 80f * 80f;
    private int lastDrawFrame;

    // Shared across all instances to avoid GC alloc per frame per robot
    private static readonly System.Collections.Generic.List<RaycastResult> sharedRaycastResults = new();
    private bool isOverUI;
    private float uiCheckTimer;

    private void Start()
    {
        if (energy == null) energy = GetComponentInParent<RobotEnergy>();
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (energy == null || mainCam == null) return;
        
        visCheckTimer -= Time.deltaTime;
        if (visCheckTimer <= 0f)
        {
            visCheckTimer = VIS_CHECK_INTERVAL;
            float sqrDist = (mainCam.transform.position - transform.position).sqrMagnitude;
            isVisible = sqrDist < MAX_RENDER_DIST_SQR;
        }

        if (isVisible)
        {
            uiCheckTimer -= Time.deltaTime;
            if (uiCheckTimer <= 0f)
            {
                uiCheckTimer = 0.5f;
                // Approximate screen position for UI overlap check
                Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + offset);
                float x = screenPos.x - barSize.x / 2;
                float y = Screen.height - screenPos.y - barSize.y / 2;
                isOverUI = IsOverCanvasUI(new Rect(x, y, barSize.x, barSize.y));
            }
        }
    }

    private void OnGUI()
    {
        if (energy == null || mainCam == null) return;
        // Only draw every other frame to halve per-robot OnGUI cost
        int frame = Time.frameCount;
        if (Event.current.type == EventType.Repaint && (frame & 1) != (lastDrawFrame & 1))
            return;
        lastDrawFrame = frame;

        if (!isVisible) return;

        Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + offset);
        if (screenPos.z < 0) return;

        float x = screenPos.x - barSize.x / 2;
        float y = Screen.height - screenPos.y - barSize.y / 2;

        Rect barRect = new Rect(x, y, barSize.x, barSize.y);

        if (isOverUI) return;

        float pct = energy.BatteryPercent;
        Color barColor = pct > 0.5f ? Color.green : (pct > 0.2f ? Color.yellow : Color.red);
        
        if (energy.IsCharging)
        {
            float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
            barColor = Color.Lerp(barColor, Color.cyan, pulse * 0.7f);
        }

        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(barRect, Texture2D.whiteTexture);
        
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, barSize.x * pct, barSize.y), Texture2D.whiteTexture);
        
        if (energy.IsCharging)
        {
            if (chargingStyle == null)
            {
                chargingStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                chargingStyle.normal.textColor = Color.white;
            }
            GUI.color = Color.white;
            GUI.Label(barRect, "CHARGING", chargingStyle);
        }

        GUI.color = Color.white;
    }

    private bool IsOverCanvasUI(Rect barRect)
    {
        if (EventSystem.current == null) return false;
        Vector2 center = new Vector2(barRect.x + barRect.width * 0.5f, barRect.y + barRect.height * 0.5f);
        var pointer = new PointerEventData(EventSystem.current) { position = center };
        sharedRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, sharedRaycastResults);
        return sharedRaycastResults.Count > 0;
    }
}
