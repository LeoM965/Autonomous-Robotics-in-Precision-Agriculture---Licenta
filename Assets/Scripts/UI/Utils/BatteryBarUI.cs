using UnityEngine;

public class BatteryBarUI : MonoBehaviour
{
    [SerializeField] private RobotEnergy energy;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private Vector2 barSize = new Vector2(100f, 15f);
    
    private Camera mainCam;
    private bool isVisible;
    private float visCheckTimer;
    private const float VIS_CHECK_INTERVAL = 0.25f;
    private const float MAX_RENDER_DIST_SQR = 80f * 80f;

    private void Start()
    {
        if (energy == null) energy = GetComponentInParent<RobotEnergy>();
        mainCam = Camera.main;
    }

    private void OnGUI()
    {
        if (energy == null || mainCam == null) return;

        visCheckTimer -= Time.deltaTime;
        if (visCheckTimer <= 0f)
        {
            visCheckTimer = VIS_CHECK_INTERVAL;
            float sqrDist = (mainCam.transform.position - transform.position).sqrMagnitude;
            isVisible = sqrDist < MAX_RENDER_DIST_SQR;
        }

        if (!isVisible) return;

        Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + offset);
        if (screenPos.z < 0) return;

        float pct = energy.BatteryPercent;
        Color barColor = pct > 0.5f ? Color.green : (pct > 0.2f ? Color.yellow : Color.red);
        
        float x = screenPos.x - barSize.x / 2;
        float y = Screen.height - screenPos.y - barSize.y / 2;

        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, barSize.x, barSize.y), Texture2D.whiteTexture);
        
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, barSize.x * pct, barSize.y), Texture2D.whiteTexture);
        
        GUI.color = Color.white;
    }
}
