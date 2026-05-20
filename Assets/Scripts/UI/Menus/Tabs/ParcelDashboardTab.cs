using UnityEngine;
using Sensors.Components;
using System.Collections.Generic;
using UI.Utils;

namespace UI.Menus.Tabs
{
    public class ParcelDashboardTab : BaseDashboardTab
    {
        private string zoneFilter = "Toate";
        private readonly float[] colOffsets = { 0, 60, 105, 190, 280, 345, 560 };


        public override void DrawTab(float x, float y, UITheme theme)
        {
            DrawZoneButtons(x, y - 2, theme);
            y += 30;

            MapHelper.DrawBox(new Rect(x - 5, y - 2, 720, 20), new Color(1f, 1f, 1f, 0.05f));
            DrawTableHeader(x, y, theme);
            y += 22;
            UIDrawUtils.DrawHorizontalLine(x, y - 2, 720);
            y += 2;

            var parcels = ParcelCache.Parcels;
            if (parcels == null || parcels.Count == 0)
            {
                GUI.Label(new Rect(x, y, 400, 20), "Nicio parcelă detectată.", theme.Label);
                return;
            }

            // Gather filtered parcels
            var filtered = new List<EnvironmentalSensor>();
            foreach (var p in parcels)
            {
                if (p == null) continue;
                string zone = GetZone(p.name);
                if (zoneFilter != "Toate" && zone != zoneFilter) continue;
                filtered.Add(p);
            }

            // Sort by name
            filtered.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            float scrollH = Mathf.Min(400f, filtered.Count * 22f + 10f);
            Rect scrollRect = new Rect(x - 5, y, 725, scrollH);
            Rect viewRect = new Rect(0, 0, 705, filtered.Count * 22f);

            scrollPos = GUI.BeginScrollView(scrollRect, scrollPos, viewRect);

            float ry = 0;
            for (int i = 0; i < filtered.Count; i++)
            {
                var parcel = filtered[i];

                if (i % 2 == 0)
                    MapHelper.DrawBox(new Rect(0, ry - 1, 705, 20), new Color(1f, 1f, 1f, 0.025f));

                DrawParcelRow(x - (x - 5), ry, parcel, theme, i);
                ry += 22;
            }

            GUI.EndScrollView();
        }

        private void DrawZoneButtons(float x, float y, UITheme theme)
        {
            string[] zones = { "Toate", "A", "B", "C", "D" };
            float bw = 55f;
            float startX = x + 720 - zones.Length * (bw + 2);

            for (int i = 0; i < zones.Length; i++)
            {
                Rect r = new Rect(startX + i * (bw + 2), y, bw, 22);
                bool active = zoneFilter == zones[i];
                Color bg = active ? new Color(theme.panelBorder.r, theme.panelBorder.g, theme.panelBorder.b, 0.3f)
                                  : new Color(1f, 1f, 1f, 0.04f);
                MapHelper.DrawBox(r, bg);
                if (active)
                    MapHelper.DrawBox(new Rect(r.x, r.yMax - 2, r.width, 2), theme.panelBorder);
                if (GUI.Button(r, zones[i], active ? theme.Value : theme.Label))
                    zoneFilter = zones[i];
            }
        }

        private void DrawTableHeader(float x, float y, UITheme theme)
        {
            string[] headers = { "ID", "pH", "Umid / Opt", "N/P/K", "Calitate", "Status", "Recomandare ML" };
            for (int i = 0; i < headers.Length; i++)
            {
                float w = (i < colOffsets.Length - 1) ? (colOffsets[i + 1] - colOffsets[i]) : 140f;
                GUI.Label(new Rect(x + colOffsets[i], y, w, 16), headers[i], theme.Value);
            }
        }

        private void DrawParcelRow(float rx, float ry, EnvironmentalSensor parcel, UITheme theme, int idx)
        {
            // ID — clickable button to center camera and close menu
            string shortName = parcel.name.Replace("Parcel_", "P_");
            if (GUI.Button(new Rect(rx + colOffsets[0], ry - 2, 55, 18), shortName, theme.Good))
            {
                var robotCam = Object.FindFirstObjectByType<RobotCamera>();
                if (robotCam != null)
                {
                    robotCam.SetTarget(parcel.transform);
                }
                var pauseMenu = Object.FindFirstObjectByType<PauseMenu>();
                if (pauseMenu != null)
                {
                    pauseMenu.TogglePause();
                }
            }

            // pH
            GUI.Label(new Rect(rx + colOffsets[1], ry, 45, 16), parcel.soilPH.ToString("F1"), theme.Label);

            // Moisture (current vs optimal)
            bool isEmpty = string.IsNullOrEmpty(parcel.plantedVarietyName) || parcel.plantedVarietyName == "None";
            float? optMoisture = null;
            if (!isEmpty)
            {
                var cropData = CropLoader.Load()?.Get(parcel.plantedVarietyName);
                if (cropData != null && cropData.requirements != null && cropData.requirements.humidity != null)
                {
                    optMoisture = cropData.requirements.humidity.optimal;
                }
            }
            string moistureText = optMoisture.HasValue 
                ? $"{parcel.soilMoisture:F0}% / {optMoisture.Value:F0}%" 
                : $"{parcel.soilMoisture:F0}%";
            GUI.Label(new Rect(rx + colOffsets[2], ry, 80, 16), moistureText, theme.Label);

            // N / P / K
            string npk = $"{parcel.nitrogen:F0}/{parcel.phosphorus:F0}/{parcel.potassium:F0}";
            GUI.Label(new Rect(rx + colOffsets[3], ry, 85, 16), npk, theme.Label);

            // Quality
            float q = parcel.soilQuality;
            GUIStyle qStyle = q >= 70 ? theme.Good : (q >= 40 ? theme.Value : theme.Bad);
            GUI.Label(new Rect(rx + colOffsets[4], ry, 60, 16), q.ToString("F0") + "%", qStyle);

            // Status — planted crop + growth OR "Liberă"
            if (isEmpty)
            {
                GUI.Label(new Rect(rx + colOffsets[5], ry, 210, 16), "Liberă", theme.Label);
            }
            else
            {
                string stageName = parcel.currentGrowthStage.ToString();
                float progress = parcel.growthProgress;
                float eff = parcel.growthEfficiency;
                string status = $"{parcel.plantedVarietyName} — {stageName} ({progress:F0}% | Pot. {eff:F0}%)";
                GUIStyle cropStyle = progress >= 100f ? theme.Good : theme.Value;
                GUI.Label(new Rect(rx + colOffsets[5], ry, 210, 16), status, cropStyle);
            }

            // ML Prediction
            var pred = AI.ML.CropMLPredictor.Predict(parcel);
            if (pred != null)
            {
                string ml = $"{pred.variety} ({pred.confidence * 100f:F0}%)";
                GUI.Label(new Rect(rx + colOffsets[6], ry, 140, 16), ml, theme.Label);
            }
            else
            {
                GUI.Label(new Rect(rx + colOffsets[6], ry, 140, 16), "—", theme.Label);
            }
        }

        private string GetZone(string parcelName)
        {
            // Parcel_A1, Parcel_B12, etc.
            string clean = parcelName.Replace("Parcel_", "");
            if (clean.Length > 0 && char.IsLetter(clean[0]))
                return clean[0].ToString();
            return "?";
        }
    }
}
