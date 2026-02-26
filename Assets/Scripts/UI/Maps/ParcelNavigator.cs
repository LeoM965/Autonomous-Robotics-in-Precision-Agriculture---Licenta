using UnityEngine;
using Sensors.Components;

public class ParcelNavigator : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey = KeyCode.G;
    [SerializeField] UITheme theme;
    string input = "";
    bool visible;
    string message = "";
    float messageTimer;
    RobotCamera robotCam;
    void Start() => robotCam = FindFirstObjectByType<RobotCamera>();
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
            if (visible) input = "";
        }
        if (messageTimer > 0) messageTimer -= Time.deltaTime;
    }
    void OnGUI()
    {
        if (!visible) return;
        var t = theme != null ? theme : UITheme.Default;
        float h = messageTimer > 0 ? 90 : 70;
        Rect panel = new Rect(Screen.width / 2 - 110, 10, 220, h);
        t.DrawPanel(panel);
        float x = panel.x + 10, y = panel.y + 8;
        GUI.Label(new Rect(x, y, 200, 18), "Navigare Parcela (ex: B4)", t.Title);
        y += 24;
        GUI.SetNextControlName("ParcelInput");
        input = GUI.TextField(new Rect(x, y, 140, 24), input.ToUpper(), 10);
        if (GUI.Button(new Rect(x + 145, y, 55, 24), "Du-te"))
            Navigate();
        if (Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == KeyCode.Return &&
            GUI.GetNameOfFocusedControl() == "ParcelInput")
        {
            Navigate();
            Event.current.Use();
        }
        if (messageTimer > 0)
        {
            y += 28;
            bool isError = message.StartsWith("!");
            GUI.Label(new Rect(x, y, 200, 16), message.TrimStart('!'), isError ? t.Warn : t.Good);
        }
    }
    void Navigate()
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        string searchName = "Parcel_" + input.Trim().ToUpper();
        EnvironmentalSensor found = null;
        foreach (var p in ParcelCache.Parcels)
        {
            if (p != null && p.name.Equals(searchName, System.StringComparison.OrdinalIgnoreCase))
            {
                found = p;
                break;
            }
        }
        if (found != null)
        {
            GoToParcel(found.transform);
            message = "Navigat la " + input.ToUpper();
            messageTimer = 2f;
            input = "";
        }
        else
        {
            message = "!Parcela " + input.ToUpper() + " nu exista!";
            messageTimer = 3f;
        }
    }
    void GoToParcel(Transform parcel)
    {
        if (robotCam != null)
        {
            robotCam.SetTarget(parcel);
            visible = false;
            return;
        }
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 pos = parcel.position + new Vector3(0, 25f, -15f);
            cam.transform.position = pos;
            cam.transform.LookAt(parcel.position);
            visible = false;
        }
    }
}
