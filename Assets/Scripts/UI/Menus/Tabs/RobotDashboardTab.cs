using UnityEngine;
using System.Collections.Generic;
using Economics.Managers;
using UI.Utils;
using Economics.Models;

namespace UI.Menus.Tabs
{
    public class RobotDashboardTab : BaseDashboardTab
    {
        private string filterZone = "Toate";
        private readonly float[] robotColOffsets = { 0, 40, 105, 195, 255, 305, 370, 435, 505 };


        private struct CachedRobotData
        {
            public string Zone;
            public string Type;
            public string StatusText;
            public float BatteryPct;
            public float WorkHours;
            public float DistKm;
            public float TotalCost;
            public float RevenueGenerated;
            public float ROI;
        }
        private List<CachedRobotData> cachedRobots = new List<CachedRobotData>();
        private float fleetInvestment;

        public void CacheRobotData()
        {
            cachedRobots.Clear();
            fleetInvestment = 0f;
            if (RobotEconomicsManager.Instance == null) return;

            var robotStatsMap = RobotEconomicsManager.Instance.RobotStatsMap;
            var sortedRobots = new List<KeyValuePair<Transform, RobotStats>>(robotStatsMap);
            sortedRobots.Sort((a, b) => {
                int zoneCompare = a.Value.zone.CompareTo(b.Value.zone);
                if (zoneCompare != 0) return zoneCompare;
                return a.Value.type.CompareTo(b.Value.type);
            });

            foreach (var kvp in sortedRobots)
            {
                var stats = kvp.Value;
                var robotTransform = kvp.Key;
                
                string statusText = "Idle";
                float batteryPct = 100f;

                if (robotTransform != null)
                {
                    var op = robotTransform.GetComponent<RobotOperator>();
                    if (op != null)
                        statusText = op.CurrentState.ToString();

                    var energy = robotTransform.GetComponent<RobotEnergy>();
                    if (energy != null) batteryPct = energy.BatteryPercent * 100f;
                }

                fleetInvestment += stats.purchasePrice;

                cachedRobots.Add(new CachedRobotData
                {
                    Zone = stats.zone,
                    Type = stats.type,
                    StatusText = statusText,
                    BatteryPct = batteryPct,
                    WorkHours = stats.time / 3600f,
                    DistKm = stats.distance / 1000f,
                    TotalCost = stats.TotalCost,
                    RevenueGenerated = stats.revenueGenerated,
                    ROI = stats.ROI
                });
            }
        }

        public override void DrawTab(float x, float y, UITheme theme) => DrawTab(x, y, theme, y + Screen.height * 0.5f);

        public void DrawTab(float x, float y, UITheme theme, float contentBottom)
        {
            // Fleet investment summary
            GUI.Label(new Rect(x, y, 200, 20), "Investiție Flotă:", theme.Label);
            GUI.Label(new Rect(x + 130, y, 150, 20), $"{fleetInvestment:N0}€", theme.Value);
            GUI.Label(new Rect(x + 310, y, 200, 20), $"({cachedRobots.Count} roboți)", theme.Label);
            y += 25;

            DrawSectionTitle(x, ref y, "TABEL ANALIZĂ FLOTĂ ROBOȚI", theme);
            y += 5;

            // Zone filter buttons
            float bx = x + 340;
            string[] zones = { "Toate", "A", "B", "C", "D" };
            foreach (var z in zones)
            {
                if (GUI.Button(new Rect(bx, y, 45, 18), z, filterZone == z ? theme.Value : theme.Label))
                    filterZone = z;
                bx += 48;
            }
            y += 25;

            // Header row
            string[] headers = { "Zone", "Model", "Activity", "Bat.", "Hours", "Dist(km)", "Cost(€)", "Venit(€)", "ROI %" };
            UIDrawUtils.DrawRow(x, y, robotColOffsets, headers, theme.Value);
            y += 22;
            UIDrawUtils.DrawHorizontalLine(x, y, 580);
            y += 8;

            // Scrollable robot list
            int visibleCount = 0;
            foreach (var data in cachedRobots)
                if (filterZone == "Toate" || data.Zone == filterZone) visibleCount++;

            float rowHeight = 18f;
            float listHeight = visibleCount * rowHeight;
            float availableHeight = contentBottom - y - 10f;
            if (availableHeight < 100f) availableHeight = 100f;
            Rect scrollView = new Rect(x, y, 590, Mathf.Min(listHeight + 10, availableHeight));
            Rect scrollContent = new Rect(0, 0, 575, listHeight);

            scrollPos = GUI.BeginScrollView(scrollView, scrollPos, scrollContent);
            float sy = 0;

            foreach (var data in cachedRobots)
            {
                if (filterZone != "Toate" && data.Zone != filterZone) continue;

                GUIStyle batStyle = data.BatteryPct > 60 ? theme.Good : (data.BatteryPct > 25 ? theme.Warn : theme.Bad);
                GUIStyle statusStyle = theme.Label;
                if (data.StatusText == "Working" || data.StatusText == "MovingToParcel") statusStyle = theme.Good;
                else if (data.StatusText == "Charging") statusStyle = theme.Bad;
                else if (data.StatusText != "Idle") statusStyle = theme.Warn;

                string[] values = {
                    $"[{data.Zone}]",
                    data.Type,
                    data.StatusText,
                    $"{data.BatteryPct:F0}%",
                    data.WorkHours.ToString("F2") + "h",
                    data.DistKm.ToString("F2") + "km",
                    data.TotalCost.ToString("F1") + "€",
                    data.RevenueGenerated.ToString("F1") + "€",
                    data.ROI.ToString("F2") + "%"
                };

                GUIStyle[] styles = {
                    theme.Good,
                    theme.Label,
                    statusStyle,
                    batStyle,
                    theme.Label,
                    theme.Label,
                    theme.Label,
                    theme.Good,
                    theme.GetProfitStyle(data.ROI)
                };

                UIDrawUtils.DrawRow(0, sy, robotColOffsets, values, styles);
                sy += rowHeight;
            }
            GUI.EndScrollView();
        }
    }
}
