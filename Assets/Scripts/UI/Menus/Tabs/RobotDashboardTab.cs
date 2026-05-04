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
        // Mai mult spațiu pentru Activity (180 la 330 = 150px lățime)
        private readonly float[] robotColOffsets = { 0, 40, 180, 330, 380, 440, 510, 580, 650 };

        private struct CachedRobotData
        {
            public string Zone, Type, StatusText;
            public float BatteryPct, WorkHours, DistKm, TotalCost, RevenueGenerated, ROI;
            public Transform RobotTransform;
        }
        private List<CachedRobotData> cachedRobots = new List<CachedRobotData>();
        private float fleetInvestment;

        // ── Decision history ──
        private int selectedRobotIndex = -1;
        private readonly DecisionLogRenderer decisionLog = new DecisionLogRenderer();

        // ── Selection indicator color ──
        private static readonly Color SelectionAccent = new Color(0.2f, 0.85f, 0.45f, 1f);
        private static readonly Color SelectionGlow   = new Color(0.1f, 0.6f, 0.3f, 0.10f);

        private GUIStyle hintActiveStyle;

        public void CacheRobotData()
        {
            cachedRobots.Clear();
            fleetInvestment = 0f;
            if (RobotEconomicsManager.Instance == null) return;

            var sortedRobots = new List<KeyValuePair<Transform, RobotStats>>(RobotEconomicsManager.Instance.RobotStatsMap);
            sortedRobots.Sort((a, b) => {
                int z = a.Value.zone.CompareTo(b.Value.zone);
                return z != 0 ? z : a.Value.type.CompareTo(b.Value.type);
            });

            foreach (var kvp in sortedRobots)
            {
                var stats = kvp.Value;
                var rt = kvp.Key;
                
                string statusText = "Idle";
                float batteryPct = 100f;

                if (rt != null)
                {
                    var energy = rt.GetComponent<RobotEnergy>();
                    if (energy != null) batteryPct = energy.BatteryPercent * 100f;

                    var operators = rt.GetComponents<RobotOperator>();
                    foreach (var op in operators)
                    {
                        if (op.CurrentState != RobotOperator.OperatorState.Idle)
                        {
                            statusText = op.GetStatus();
                            break;
                        }
                    }

                    if (statusText == "Idle")
                    {
                        var flight = rt.GetComponent<Robots.Capabilities.Flight.AgroBotFlight>();
                        if (flight != null)
                        {
                            string fStatus = flight.GetStatus();
                            if (!fStatus.StartsWith("Idle")) statusText = fStatus;
                        }
                    }

                    if (statusText == "Idle" && energy != null && energy.IsCharging)
                        statusText = "Charging";
                }

                string robotName = rt != null ? rt.name.Replace("(Clone)", "").Trim() : stats.type;

                fleetInvestment += stats.purchasePrice;
                cachedRobots.Add(new CachedRobotData
                {
                    Zone = stats.zone, Type = robotName, StatusText = statusText,
                    BatteryPct = batteryPct, WorkHours = stats.time / 3600f,
                    DistKm = stats.distance / 1000f, TotalCost = stats.TotalCost,
                    RevenueGenerated = stats.revenueGenerated, ROI = stats.ROI,
                    RobotTransform = rt
                });
            }

            if (selectedRobotIndex >= cachedRobots.Count)
                selectedRobotIndex = -1;
        }

        public override void DrawTab(float x, float y, UITheme theme) => DrawTab(x, y, theme, y + Screen.height * 0.5f);

        public void DrawTab(float x, float y, UITheme theme, float contentBottom)
        {
            EnsureHintStyle(theme);

            MapHelper.DrawBox(new Rect(x - 5, y - 3, 550, 24), new Color(1f, 1f, 1f, 0.04f));
            GUI.Label(new Rect(x, y, 200, 20), "Investiție Flotă:", theme.Label);
            GUI.Label(new Rect(x + 140, y, 150, 20), $"{fleetInvestment:N0}€", theme.Value);
            GUI.Label(new Rect(x + 340, y, 200, 20), $"({cachedRobots.Count} roboți)", theme.Label);
            y += 30;

            DrawSectionTitle(x, ref y, "TABEL ANALIZĂ FLOTĂ ROBOȚI", theme);
            y += 10;

            // ── Zone filter buttons ──
            float bx = x + 440;
            string[] zones = { "Toate", "A", "B", "C", "D" };
            foreach (var z in zones)
            {
                Rect btnRect = new Rect(bx, y, 50, 20);
                bool isActive = filterZone == z;
                if (isActive)
                    MapHelper.DrawBox(btnRect, new Color(theme.panelBorder.r, theme.panelBorder.g, theme.panelBorder.b, 0.25f));
                if (GUI.Button(btnRect, z, isActive ? theme.Value : theme.Label))
                    filterZone = z;
                bx += 55;
            }
            y += 35;

            // ── Table header ──
            MapHelper.DrawBox(new Rect(x - 5, y - 2, 720, 22), new Color(1f, 1f, 1f, 0.05f));
            string[] headers = { "Zone", "Model", "Activity", "Bat.", "Hours", "Dist(km)", "Cost(€)", "Venit(€)", "ROI %" };
            UIDrawUtils.DrawRow(x, y, robotColOffsets, headers, theme.Value);
            y += 26;
            UIDrawUtils.DrawHorizontalLine(x, y, 720);
            y += 12;

            // ── Build filtered list ──
            List<int> filteredIndices = new List<int>();
            for (int i = 0; i < cachedRobots.Count; i++)
                if (filterZone == "Toate" || cachedRobots[i].Zone == filterZone)
                    filteredIndices.Add(i);

            float rowHeight = 22f;
            float listHeight = filteredIndices.Count * rowHeight;

            bool hasSelection = selectedRobotIndex >= 0 && selectedRobotIndex < cachedRobots.Count;
            float historyHeight = hasSelection ? 280f : 0f;
            float availableHeight = Mathf.Max(contentBottom - y - 10f - historyHeight, 100f);

            Rect scrollView = new Rect(x, y, 730, Mathf.Min(listHeight + 15, availableHeight));
            Rect scrollContent = new Rect(0, 0, 715, listHeight);

            scrollPos = GUI.BeginScrollView(scrollView, scrollPos, scrollContent);
            float sy = 0;
            int rowIndex = 0;

            foreach (int idx in filteredIndices)
            {
                var data = cachedRobots[idx];
                bool isSelected = idx == selectedRobotIndex;

                if (isSelected)
                    MapHelper.DrawBox(new Rect(-5, sy - 1, 720, 20), SelectionGlow);
                else if (rowIndex % 2 == 0)
                    MapHelper.DrawBox(new Rect(-5, sy - 1, 720, 20), new Color(1f, 1f, 1f, 0.025f));

                if (GUI.Button(new Rect(-5, sy - 1, 720, 20), GUIContent.none, GUIStyle.none))
                    selectedRobotIndex = (selectedRobotIndex == idx) ? -1 : idx;

                if (isSelected)
                    MapHelper.DrawBox(new Rect(-5, sy - 1, 3, 20), SelectionAccent);

                GUIStyle batStyle = data.BatteryPct > 60 ? theme.Good : (data.BatteryPct > 25 ? theme.Warn : theme.Bad);
                GUIStyle statusStyle = theme.Label;
                if (data.StatusText == "Working" || data.StatusText == "MovingToParcel") statusStyle = theme.Good;
                else if (data.StatusText == "Charging") statusStyle = theme.Bad;
                else if (data.StatusText != "Idle") statusStyle = theme.Warn;

                string[] values = {
                    $"[{data.Zone}]", data.Type, data.StatusText, $"{data.BatteryPct:F0}%",
                    data.WorkHours.ToString("F2") + "h", data.DistKm.ToString("F2") + "km",
                    data.TotalCost.ToString("F1") + "€", data.RevenueGenerated.ToString("F1") + "€",
                    data.ROI.ToString("F2") + "%"
                };

                GUIStyle[] styles = {
                    theme.Good, theme.Label, statusStyle, batStyle,
                    theme.Label, theme.Label, theme.Label, theme.Good,
                    theme.GetProfitStyle(data.ROI)
                };

                UIDrawUtils.DrawRow(0, sy, robotColOffsets, values, styles);
                sy += rowHeight;
                rowIndex++;
            }
            GUI.EndScrollView();

            y += scrollView.height + 8;

            // ── Hint text ──
            string hint = hasSelection
                ? "▼ Click alt robot pentru a schimba  │  Click din nou pentru a închide"
                : "Click pe un robot pentru a vedea istoricul deciziilor";
            GUI.Label(new Rect(x, y, 500, 16), hint, hasSelection ? hintActiveStyle : theme.Label);
            y += 22;

            // ── Decision Log (delegated to renderer) ──
            hasSelection = selectedRobotIndex >= 0 && selectedRobotIndex < cachedRobots.Count;
            if (hasSelection)
            {
                float maxLogH = Mathf.Max(contentBottom - y - 15f, 100f);
                decisionLog.Draw(x, y, cachedRobots[selectedRobotIndex].RobotTransform, maxLogH, theme);
            }
        }

        private void EnsureHintStyle(UITheme theme)
        {
            if (hintActiveStyle != null) return;
            hintActiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleLeft
            };
            hintActiveStyle.normal.textColor = new Color(0.45f, 0.55f, 0.65f, 0.7f);
        }

        /// <summary>Extra panel height required when decision log is showing.</summary>
        public float ExtraHeight => selectedRobotIndex >= 0 ? 300f : 0f;
    }
}
