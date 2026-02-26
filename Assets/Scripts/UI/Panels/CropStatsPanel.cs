using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class CropStatsPanel : MonoBehaviour
{
    [SerializeField] UITheme theme;
    [SerializeField] KeyCode toggleKey = KeyCode.F2;
    Dictionary<string, Dictionary<string, int>> zones = new();
    int totalPlants;
    bool visible = true;
    float timer;
    readonly List<string> sortedKeys = new List<string>();
    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) visible = !visible;
        if ((timer -= Time.deltaTime) <= 0f) { Refresh(); timer = 2f; }
    }
    void Refresh()
    {
        zones.Clear();
        totalPlants = 0;
        foreach (var p in ParcelCache.Parcels)
        {
            if (p == null) continue;
            string zone = GetZone(p.name);
            string crop = p.plantedVarietyName;
            int count = 0;
            if (p.activeCrops != null) count = p.activeCrops.Count;
            totalPlants += count;
            if (!zones.TryGetValue(zone, out var crops))
                zones[zone] = crops = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(crop))
                crops[crop] = crops.TryGetValue(crop, out int c) ? c + count : count;
        }
    }
    string GetZone(string name)
    {
        int i = name.IndexOf('_');
        return (i >= 0 && i + 1 < name.Length) ? name[i + 1].ToString().ToUpper() : "?";
    }
    void OnGUI()
    {
        if (!visible) return;
        var t = theme != null ? theme : UITheme.Default;
        int rows = 0;
        foreach (var z in zones.Values) rows += Mathf.Max(1, z.Count);
        Rect panel = new Rect(10, 300, 220, 60 + rows * 20 + zones.Count * 6);
        t.DrawPanel(panel);
        float x = panel.x + 10, y = panel.y + 8;
        GUI.Label(new Rect(x, y, 200, 20), "Plante Cultivate", t.Title);
        y += 28;
        sortedKeys.Clear();
        sortedKeys.AddRange(zones.Keys);
        sortedKeys.Sort();
        foreach (var zone in sortedKeys)
        {
            var crops = zones[zone];
            bool first = true;
            if (crops.Count == 0)
            {
                Row(ref y, x, zone, "neplantat", "", t, first, true);
            }
            else
            {
                foreach (var kv in crops)
                {
                    Row(ref y, x, zone, kv.Key, kv.Value.ToString(), t, first, false);
                    first = false;
                }
            }
            y += 4;
        }
        y += 2;
        GUI.Label(new Rect(x, y, 155, 16), "TOTAL PLANTE", t.Value);
        GUI.Label(new Rect(x + 165, y, 45, 16), totalPlants.ToString(), t.Value);
    }
    void Row(ref float y, float x, string zone, string crop, string count, UITheme t, bool showZone, bool warn)
    {
        if (showZone) GUI.Label(new Rect(x, y, 35, 16), $"[{zone}]", t.Good);
        GUI.Label(new Rect(x + 40, y, 120, 16), crop, warn ? t.Warn : t.Label);
        if (!string.IsNullOrEmpty(count)) GUI.Label(new Rect(x + 165, y, 45, 16), count, t.Value);
        y += 18;
    }
}
