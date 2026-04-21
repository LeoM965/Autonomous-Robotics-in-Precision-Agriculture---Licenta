using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sensors.Components;
using TMPro;

namespace UI.Canvas
{
    public class CropStats : MonoBehaviour
    {
        private Transform container;
        private RectTransform containerRect;
        private TextMeshProUGUI totalTxt;
        private Dictionary<string, Dictionary<string, int>> zones = new Dictionary<string, Dictionary<string, int>>();
        private List<Row> pool = new List<Row>();
        private int activeCount;
        private int lastActiveCount;
        private float timer;

        private class Row
        {
            public GameObject obj;
            public TextMeshProUGUI zone, crop, count;
            public void Set(string z, string c, string cnt, bool showZone, bool warn) 
            {
                obj.SetActive(true);
                zone.text = showZone ? $"[{z}]" : "";
                crop.text = c;
                crop.color = warn ? CanvasHelper.Warning : CanvasHelper.Subtitle;
                count.text = cnt;
            }
        }

        void Start()
        {
            container = transform.Find("Visuals/List");
            containerRect = container?.GetComponent<RectTransform>();
            totalTxt = transform.Find("Visuals/Total")?.GetComponent<TextMeshProUGUI>();
            Refresh();
        }

        void Update()
        {
            if ((timer -= Time.deltaTime) <= 0f) { Refresh(); timer = 2f; }
        }

        private void Refresh()
        {
            if (!container) return;
            activeCount = 0;
            zones.Clear();
            int total = 0;

            foreach (var p in ParcelCache.Parcels)
            {
                if (p == null) continue;
                string z = GetZone(p.name);
                int count = p.activeCrops?.Count ?? 0;
                total += count;

                if (!zones.TryGetValue(z, out var crs)) zones[z] = crs = new Dictionary<string, int>();
                if (!string.IsNullOrEmpty(p.plantedVarietyName))
                    crs[p.plantedVarietyName] = crs.TryGetValue(p.plantedVarietyName, out int v) ? v + count : count;
            }

            var keys = new List<string>(zones.Keys);
            keys.Sort();
            foreach (var k in keys)
            {
                var crs = zones[k];
                bool first = true;
                if (crs.Count == 0) UpdateRow(k, "neplantat", "", first, true);
                else foreach (var kv in crs) { UpdateRow(k, kv.Key, kv.Value.ToString(), first, false); first = false; }
            }

            for (int i = activeCount; i < pool.Count; i++) pool[i].obj.SetActive(false);
            if (totalTxt) totalTxt.text = $"TOTAL PLANTE: {total}";
            if (activeCount != lastActiveCount && containerRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
                lastActiveCount = activeCount;
            }
        }

        private void UpdateRow(string z, string c, string cnt, bool sz, bool w)
        {
            Row r;
            if (activeCount < pool.Count) r = pool[activeCount];
            else
            {
                GameObject go = new GameObject("R");
                go.transform.SetParent(container, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(260, 20);
                go.AddComponent<LayoutElement>().preferredHeight = 20;

                r = new Row { obj = go };
                r.zone = CanvasHelper.AddText(go.transform, "", CanvasHelper.Good, 10, FontStyles.Bold, Vector2.zero, new Vector2(35, 20));
                r.crop = CanvasHelper.AddText(go.transform, "", CanvasHelper.Subtitle, 10, FontStyles.Normal, new Vector2(40, 0), new Vector2(150, 20));
                r.count = CanvasHelper.AddText(go.transform, "", CanvasHelper.Value, 10, FontStyles.Bold, new Vector2(200, 0), new Vector2(60, 20), "V", TextAlignmentOptions.Right);
                pool.Add(r);
            }
            r.Set(z, c, cnt, sz, w);
            activeCount++;
        }

        private string GetZone(string n) { int i = n.IndexOf('_'); return (i >= 0 && i + 1 < n.Length) ? n[i + 1].ToString().ToUpper() : "?"; }
    }
}
