using UnityEngine;
using System.Collections.Generic;
using AI.Analytics;
using UI.Utils;

namespace UI.Menus.Tabs
{
    public class DecisionLogRenderer
    {
        private Vector2 scrollPos;

        // ── Cached styles (built once from theme) ──
        private GUIStyle headerStyle, colHeaderStyle, rowStyle, rowBoldStyle,
                         positiveStyle, negativeStyle, dimStyle, footerStyle, badgeStyle;
        private UITheme cachedTheme;

        // ── Layout constants ──
        private static readonly float[] ColOffsets = { 0, 50, 125, 275, 425, 510, 605 };
        private static readonly string[] ColHeaders = { "#", "SIM", "ACȚIUNE", "PARCELĂ", "SCOR", "PRIO", "NET VALUE" };
        private const float RowHeight = 22f;
        private const float HeaderHeight = 32f;
        private const float ColHeaderHeight = 20f;

        // ── Colors ──
        private static readonly Color PanelBG        = new Color(0.04f, 0.06f, 0.10f, 0.96f);
        private static readonly Color PanelBorder     = new Color(0.15f, 0.55f, 0.85f, 0.45f);
        private static readonly Color HeaderBG        = new Color(0.08f, 0.14f, 0.22f, 0.9f);
        private static readonly Color RowAlt          = new Color(1f, 1f, 1f, 0.025f);
        private static readonly Color RowNewest       = new Color(0.1f, 0.6f, 0.3f, 0.12f);
        private static readonly Color AccentIndicator = new Color(0.2f, 0.85f, 0.45f, 1f);
        private static readonly Color SeparatorColor  = new Color(1f, 1f, 1f, 0.08f);

        public void Draw(float x, float y, Transform robot, float maxHeight, UITheme theme)
        {
            if (DecisionTracker.Instance == null || robot == null) return;
            EnsureStyles(theme);

            var decisions = DecisionTracker.Instance.GetRecentDecisions(robot, 1000);

            float w = 720f;
            float h = Mathf.Min(maxHeight, 400f);
            Rect panel = new Rect(x - 5, y, w, h);

            // ── Panel background and border ──
            MapHelper.DrawBox(panel, PanelBG);
            MapHelper.DrawBorder(panel, PanelBorder, 1);

            float px = x + 10;
            float py = y + 6;

            // ── Header bar ──
            Rect headerBar = new Rect(x - 5, y, w, HeaderHeight);
            MapHelper.DrawBox(headerBar, HeaderBG);

            string robotId = robot.name.Length > 22 ? robot.name.Substring(0, 22) : robot.name;
            GUI.Label(new Rect(px, py + 2, w - 30, 20),
                $"  Istoric Decizii  ─  {robotId}", headerStyle);

            // Badge: entry count
            string badge = $" {decisions.Count} ";
            float badgeW = badge.Length * 9f;
            Rect badgeRect = new Rect(x + w - badgeW - 30, py + 4, badgeW, 18);
            MapHelper.DrawBox(badgeRect, new Color(AccentIndicator.r, AccentIndicator.g, AccentIndicator.b, 0.2f));
            GUI.Label(badgeRect, badge, badgeStyle);

            py = y + HeaderHeight + 6;

            // ── Column headers ──
            for (int i = 0; i < ColHeaders.Length; i++)
            {
                float colW = (i < ColOffsets.Length - 1) ? (ColOffsets[i + 1] - ColOffsets[i]) : 100f;
                GUI.Label(new Rect(px + ColOffsets[i], py, colW, ColHeaderHeight), ColHeaders[i], colHeaderStyle);
            }
            py += ColHeaderHeight;

            // ── Separator ──
            MapHelper.DrawBox(new Rect(px, py, w - 30, 1), SeparatorColor);
            py += 4;

            // ── Scrollable entries ──
            float remainingH = Mathf.Max(h - (py - y) - 36f, 50f);
            float contentH = Mathf.Max(decisions.Count, 1) * RowHeight;

            Rect scrollRect = new Rect(px - 2, py, w - 20, remainingH);
            Rect scrollContent = new Rect(0, 0, w - 40, contentH);

            scrollPos = GUI.BeginScrollView(scrollRect, scrollPos, scrollContent);

            if (decisions.Count == 0)
            {
                GUI.Label(new Rect(4, 2, 500, RowHeight),
                    "Nu există decizii înregistrate  ─  robotul este în așteptare", dimStyle);
            }
            else
            {
                float ey = 0;
                for (int i = decisions.Count - 1; i >= 0; i--)
                {
                    DrawEntry(decisions[i], i, decisions.Count, w, ref ey);
                }
            }

            GUI.EndScrollView();

            // ── Footer ──
            float footerY = py + remainingH + 4;
            DrawFooter(px, footerY, w - 30, robot);
        }

        // ═══════════════════════════════════════
        //  ENTRY RENDERING
        // ═══════════════════════════════════════

        private void DrawEntry(DecisionRecord d, int index, int total, float w, ref float ey)
        {
            bool isNewest = (index == total - 1);
            int displayIndex = total - index;

            // Row background
            if (isNewest)
            {
                MapHelper.DrawBox(new Rect(-2, ey, w - 36, RowHeight), RowNewest);
                MapHelper.DrawBox(new Rect(-2, ey, 3, RowHeight), AccentIndicator);
            }
            else if (displayIndex % 2 == 0)
            {
                MapHelper.DrawBox(new Rect(-2, ey, w - 36, RowHeight), RowAlt);
            }

            GUIStyle lineStyle = isNewest ? rowBoldStyle : rowStyle;

            // Index (Global)
            GUI.Label(new Rect(ColOffsets[0] + 4, ey, 50, RowHeight), $"#{d.globalIndex:D3}", dimStyle);

            // Timestamp (In-game Time)
            int day = Mathf.FloorToInt(d.timestamp / 24f) + 1;
            int hour = Mathf.FloorToInt(d.timestamp % 24f);
            int min = Mathf.FloorToInt((d.timestamp * 60f) % 60f);
            GUI.Label(new Rect(ColOffsets[1] + 4, ey, 60, RowHeight), $"Z{day} {hour:D2}:{min:D2}", dimStyle);

            // Action
            string action = Truncate(d.chosenOption ?? "N/A", 18).ToUpper();
            GUI.Label(new Rect(ColOffsets[2] + 4, ey, 145, RowHeight), action, lineStyle);

            // Parcel
            string parcel = d.parcelName ?? "---";
            GUI.Label(new Rect(ColOffsets[3] + 4, ey, 130, RowHeight), Truncate(parcel, 16), lineStyle);

            // Score
            GUI.Label(new Rect(ColOffsets[4] + 4, ey, 70, RowHeight), d.chosenScore.ToString("F1"), lineStyle);

            // Priority (Scan-time Value)
            GUI.Label(new Rect(ColOffsets[5] + 4, ey, 80, RowHeight), d.schedulingValue.ToString("F3"), lineStyle);

            // Net value (color-coded)
            string sign = d.netValue >= 0 ? "+" : "";
            GUIStyle nvStyle = d.netValue >= 0 ? positiveStyle : negativeStyle;
            GUI.Label(new Rect(ColOffsets[6] + 4, ey, 95, RowHeight), $"{sign}{d.netValue:F3}", nvStyle);

            ey += RowHeight;
        }

        // ═══════════════════════════════════════
        //  FOOTER
        // ═══════════════════════════════════════

        private void DrawFooter(float px, float footerY, float w, Transform robot)
        {
            MapHelper.DrawBox(new Rect(px - 2, footerY - 2, w, 1), SeparatorColor);

            int total = DecisionTracker.Instance.GetTotalDecisions(robot);
            float avg = DecisionTracker.Instance.GetAverageScore(robot);

            GUI.Label(new Rect(px, footerY + 2, w, 16),
                $"Total Decizii: {total}   │   Scor Mediu: {avg:F1}", footerStyle);

            // Mini progress bar for avg score (0-100 range)
            float barX = px + w - 130;
            float barW = 110f;
            float barY = footerY + 5;
            float barH = 8f;
            MapHelper.DrawBox(new Rect(barX, barY, barW, barH), new Color(1f, 1f, 1f, 0.06f));
            float fill = Mathf.Clamp01(avg / 100f);
            Color barColor = fill > 0.6f ? new Color(0.2f, 0.85f, 0.4f, 0.7f) :
                             fill > 0.3f ? new Color(0.95f, 0.75f, 0.1f, 0.7f) :
                                           new Color(0.95f, 0.25f, 0.15f, 0.7f);
            MapHelper.DrawBox(new Rect(barX, barY, barW * fill, barH), barColor);
        }

        // ═══════════════════════════════════════
        //  STYLE FACTORY
        // ═══════════════════════════════════════

        private void EnsureStyles(UITheme theme)
        {
            if (headerStyle != null && cachedTheme == theme) return;
            cachedTheme = theme;

            headerStyle = MakeStyle(13, FontStyle.Bold, theme.titleColor);
            colHeaderStyle = MakeStyle(11, FontStyle.Bold, new Color(0.55f, 0.65f, 0.75f, 0.85f));
            rowStyle = MakeStyle(12, FontStyle.Normal, new Color(0.78f, 0.82f, 0.88f, 0.9f));
            rowBoldStyle = MakeStyle(12, FontStyle.Bold, Color.white);
            positiveStyle = MakeStyle(12, FontStyle.Bold, theme.goodColor);
            negativeStyle = MakeStyle(12, FontStyle.Bold, theme.badColor);
            dimStyle = MakeStyle(11, FontStyle.Normal, new Color(0.45f, 0.52f, 0.60f, 0.75f));
            footerStyle = MakeStyle(11, FontStyle.Normal, new Color(0.55f, 0.62f, 0.70f, 0.8f));
            badgeStyle = MakeStyle(11, FontStyle.Bold, AccentIndicator, TextAnchor.MiddleCenter);
        }

        private static GUIStyle MakeStyle(int size, FontStyle weight, Color color,
                                           TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = size, fontStyle = weight, alignment = anchor
            };
            s.normal.textColor = color;
            return s;
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
