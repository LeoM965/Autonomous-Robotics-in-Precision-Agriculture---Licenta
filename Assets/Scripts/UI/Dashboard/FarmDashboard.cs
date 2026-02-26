using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class FarmDashboard : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey = KeyCode.F1;
    [SerializeField] bool showOnStart = true;
    [SerializeField] UITheme theme;
    MultiRobotSpawner spawner;
    List<EnvironmentalSensor> parcels = new List<EnvironmentalSensor>();
    UITheme t;
    float avgQuality;
    float avgHumidity;
    float avgPH;
    int good, poor, critical;
    int robots;
    float timer;
    bool show;
    void Start()
    {
        show = showOnStart;
        spawner = FindFirstObjectByType<MultiRobotSpawner>();
        Invoke(nameof(Init), 0.5f);
    }
    void Init()
    {
        parcels.AddRange(ParcelCache.Parcels);
        Refresh();
    }
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            show = !show;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Refresh();
            timer = 2f;
        }
    }
    void Refresh()
    {
        if (parcels.Count == 0)
            return;
        float totalQuality = 0f;
        float totalHumidity = 0f;
        float totalPH = 0f;
        good = poor = critical = 0;
        foreach (var parcel in parcels)
        {
            if (parcel?.composition == null)
                continue;
            
            float quality = parcel.LatestAnalysis.qualityScore;
            totalQuality += quality;
            totalHumidity += parcel.composition.moisture;
            totalPH += parcel.composition.pH;
            
            if (quality >= 65f)
                good++;
            else if (quality >= 35f)
                poor++;
            else
                critical++;
        }
        int count = parcels.Count;
        avgQuality = totalQuality / count;
        avgHumidity = totalHumidity / count;
        avgPH = totalPH / count;
        robots = 0;
        if (spawner != null)
        {
            var rList = spawner.GetRobots();
            if (rList != null) robots = rList.Count;
        }
    }
    void OnGUI()
    {
        if (!show)
            return;
        t = theme != null ? theme : UITheme.Default;
        Rect panel = new Rect(Screen.width - 195, 150, 180, 200);
        t.DrawPanel(panel);
        float y = panel.y + 10;
        float x = panel.x + 12;
        GUI.Label(new Rect(x, y, 160, 18), "Statistici Ferma", t.Title);
        y += 22;
        Row(x, ref y, "Parcele", parcels.Count.ToString());
        Row(x, ref y, "Roboti", robots.ToString());
        y += 5;
        Row(x, ref y, "Calitate", $"{avgQuality:F0}%", t.GetQualityStyle(avgQuality));
        Row(x, ref y, "pH", $"{avgPH:F1}");
        Row(x, ref y, "Umiditate", $"{avgHumidity:F0}%");
        y += 5;
        Row(x, ref y, "Bune", good.ToString(), t.Good);
        Row(x, ref y, "Slabe", poor.ToString(), t.Warn);
        Row(x, ref y, "Critice", critical.ToString(), t.Bad);
    }
    void Row(float x, ref float y, string label, string value, GUIStyle style = null)
    {
        GUI.Label(new Rect(x, y, 90, 16), label, t.Label);
        GUI.Label(new Rect(x + 92, y, 70, 16), value, style != null ? style : t.Label);
        y += 17;
    }
}
