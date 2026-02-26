using UnityEngine;

public class BatteryBarUI : MonoBehaviour
{
    [SerializeField] private RobotEnergy energy;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
    [SerializeField] private Vector2 size = new Vector2(1.5f, 0.2f);
    
    private Transform camTransform;
    private Texture2D bgTexture;
    private Texture2D fillTexture;

    private void Start()
    {
        if (energy == null) energy = GetComponentInParent<RobotEnergy>();
        
        Camera cam = Camera.main;
        if (cam != null) camTransform = cam.transform;

        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, Color.gray);
        bgTexture.Apply();

        fillTexture = new Texture2D(1, 1);
        fillTexture.SetPixel(0, 0, Color.green);
        fillTexture.Apply();
    }

    private void OnGUI()
    {
        if (energy == null || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + offset);

        if (screenPos.z < 0) return;

        float pct = energy.BatteryPercent;
        Color barColor = pct > 0.5f ? Color.green : (pct > 0.2f ? Color.yellow : Color.red);
        
        float width = 100f;
        float height = 15f;
        float x = screenPos.x - width / 2;
        float y = Screen.height - screenPos.y - height / 2;

        GUI.color = Color.gray;
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, width * pct, height), Texture2D.whiteTexture);
        
        GUI.color = Color.white;
    }
}
