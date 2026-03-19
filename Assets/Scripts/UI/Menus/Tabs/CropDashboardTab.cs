using UnityEngine;
using Economics.Models;
using UI.Utils;

namespace UI.Menus.Tabs
{
    public class CropDashboardTab : BaseDashboardTab
    {
        private readonly float[] colOffsets = { 0, 110, 160, 230, 305, 380, 455, 525, 595 };

        public override void DrawTab(float x, float y, UITheme theme)
        {
            // Placeholder for base override - logic is in DrawTab overload
        }

        public void DrawTab(float x, float y, EconomicReport activeReport, CropData[] crops, UITheme theme)
        {
            DrawTableHeader(x, y, theme);
            y += 20;

            foreach (var crop in crops)
            {
                activeReport.AnalysisByVariety.TryGetValue(crop.name, out var stats);
                DrawDataRowCustom(x, y, crop.name, stats, stats.NetProfit, theme, isTotalRow: false);
                y += 17;
            }

            UIDrawUtils.DrawHorizontalLine(x, y + 4, 610);
            
            float cropOnlyProfit = activeReport.FarmTotals.TotalRevenue - activeReport.FarmTotals.TotalSeedCost;
            DrawDataRowCustom(x, y + 12, "TOTAL CULTURI", activeReport.FarmTotals, cropOnlyProfit, theme, isTotalRow: true);
            
            y += 55;
            DrawOperationalBreakdown(x, y, activeReport.FarmTotals, theme);
        }

        private void DrawTableHeader(float x, float y, UITheme theme)
        {
            string[] headers = { "Cultură", "Plant", "Cost €", "Venit €", "Kg", "Profit €", "ROI %", "Fit %" };
            UIDrawUtils.DrawRow(x, y, colOffsets, headers, theme.Value, colWidth: 65f);
        }

        private void DrawDataRowCustom(float x, float y, string label, CropStats s, float profitToShow, UITheme theme, bool isTotalRow)
        {
            GUIStyle labelStyle = isTotalRow ? theme.Value : ((s.TotalPlants + s.HarvestedPlants) > 0 ? theme.Good : theme.Label);
            GUIStyle dataStyle = isTotalRow ? theme.Value : theme.Label;

            string plantText = s.HarvestedPlants > 0 ? $"{s.TotalPlants}+{s.HarvestedPlants}" : s.TotalPlants.ToString();
            float roi = (s.TotalSeedCost > 0) ? (profitToShow / s.TotalSeedCost) * 100f : 0f;

            string[] values = {
                label,
                plantText,
                s.TotalSeedCost.ToString("F1"),
                s.TotalRevenue.ToString("F1"),
                s.TotalWeightKg.ToString("F1"),
                profitToShow.ToString("F1"),
                roi.ToString("F0") + "%",
                s.AvgSoilCompatibility.ToString("F0") + "%"
            };

            GUIStyle[] styles = {
                labelStyle,
                dataStyle,
                dataStyle,
                dataStyle,
                dataStyle,
                theme.GetProfitStyle(profitToShow),
                theme.GetProfitStyle(roi),
                theme.GetProfitStyle(s.AvgSoilCompatibility - 50f)
            };

            UIDrawUtils.DrawRow(x, y, colOffsets, values, styles, colWidth: 105f);
        }

        private void DrawOperationalBreakdown(float x, float y, CropStats totals, UITheme theme)
        {
            GUI.Label(new Rect(x, y, 320, 18), "DETALII COSTURI OPERAȚIONALE (LOGISTICĂ)", theme.Value);
            y += 26;
            
            float labelWidth = 160;
            float indent = 12;
            float valueX = x + indent + labelWidth;

            DrawSmallRow(x + indent, valueX, ref y, "Venit Recoltat:", totals.HarvestedRevenue.ToString("F2") + " €", theme.Label, theme.Good);
            DrawSmallRow(x + indent, valueX, ref y, "Venit în Câmp:", totals.FieldRevenue.ToString("F2") + " €", theme.Label, theme.Value);
            y += 4;
            DrawSmallRow(x + indent, valueX, ref y, "Consum Energie:", totals.TotalEnergyCost.ToString("F2") + " €", theme.Label, theme.Value);
            DrawSmallRow(x + indent, valueX, ref y, "Mentenanță Utilitaje:", totals.TotalMaintenanceCost.ToString("F2") + " €", theme.Label, theme.Value);
            DrawSmallRow(x + indent, valueX, ref y, "Amortizare Active:", totals.TotalDepreciationCost.ToString("F2") + " €", theme.Label, theme.Value);
            
            y += 8;
            UIDrawUtils.DrawHorizontalLine(x + indent, y, 400);
            y += 10;
            DrawSmallRow(x + indent, valueX, ref y, "PROFIT NET FINAL:", totals.NetProfit.ToString("F2") + " €", theme.Label, theme.GetProfitStyle(totals.NetProfit));
        }

        private void DrawSmallRow(float x, float valueX, ref float y, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUI.Label(new Rect(x, y, 160, 16), label, labelStyle);
            GUI.Label(new Rect(valueX, y, 120, 16), value, valueStyle);
            y += 18;
        }
    }
}
