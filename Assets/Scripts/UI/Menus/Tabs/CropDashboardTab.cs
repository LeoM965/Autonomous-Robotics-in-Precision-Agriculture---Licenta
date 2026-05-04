using UnityEngine;
using Economics.Models;
using Economics.Managers;
using UI.Utils;

namespace UI.Menus.Tabs
{
    public class CropDashboardTab : BaseDashboardTab
    {
        private readonly float[] colOffsets = { 0, 130, 200, 280, 360, 440, 520, 590, 660 };

        public override void DrawTab(float x, float y, UITheme theme) { }

        public void DrawTab(float x, float y, EconomicReport activeReport, CropData[] crops, UITheme theme)
        {
            if (GUI.Button(new Rect(x + 570, y - 35, 140, 25), "EXPORT CROP DATA"))
            {
                if (EconomicsHistoryManager.Instance != null)
                    EconomicsHistoryManager.Instance.ExportCropsToCSV();
            }

            MapHelper.DrawBox(new Rect(x - 5, y - 2, 720, 20), new Color(1f, 1f, 1f, 0.05f));
            DrawTableHeader(x, y, theme);
            y += 22;
            UIDrawUtils.DrawHorizontalLine(x, y - 2, 720);
            y += 2;

            for (int i = 0; i < crops.Length; i++)
            {
                if (i % 2 == 0)
                    MapHelper.DrawBox(new Rect(x - 5, y - 1, 720, 20), new Color(1f, 1f, 1f, 0.025f));

                activeReport.AnalysisByVariety.TryGetValue(crops[i].name, out var stats);
                float harvestProfit = stats.HarvestedRevenue - stats.TotalSeedCost;
                DrawDataRowCustom(x, y, crops[i].name, stats, harvestProfit, theme, false);
                y += 22;
            }

            UIDrawUtils.DrawHorizontalLine(x, y + 4, 720);
            
            MapHelper.DrawBox(new Rect(x - 5, y + 10, 720, 22), new Color(1f, 1f, 1f, 0.06f));
            float cropOnlyProfit = activeReport.FarmTotals.HarvestedRevenue - activeReport.FarmTotals.TotalSeedCost;
            DrawDataRowCustom(x, y + 15, "TOTAL CULTURI", activeReport.FarmTotals, cropOnlyProfit, theme, true);
            
            y += 55;
            DrawOperationalBreakdown(x, y, activeReport.FarmTotals, theme);
        }

        private void DrawTableHeader(float x, float y, UITheme theme)
        {
            string[] headers = { "Cultură", "Plante", "Cost €", "Venit €", "Kg", "Profit €", "ROI %", "Fit %" };
            UIDrawUtils.DrawRow(x, y, colOffsets, headers, theme.Value, colWidth: 75f);
        }

        private void DrawDataRowCustom(float x, float y, string label, CropStats s, float profitToShow, UITheme theme, bool isTotalRow)
        {
            int totalAll = s.TotalPlants + s.HarvestedPlants;
            GUIStyle labelStyle = isTotalRow ? theme.Value : (totalAll > 0 ? theme.Good : theme.Label);
            GUIStyle dataStyle = isTotalRow ? theme.Value : theme.Label;
            float roi = (s.TotalSeedCost > 0) ? (profitToShow / s.TotalSeedCost) * 100f : 0f;

            string[] values = {
                label, totalAll.ToString(),
                s.TotalSeedCost.ToString("F1"), s.HarvestedRevenue.ToString("F1"),
                s.TotalWeightKg.ToString("F1"), profitToShow.ToString("F1"),
                roi.ToString("F0") + "%", s.AvgSoilCompatibility.ToString("F0") + "%"
            };

            GUIStyle[] styles = {
                labelStyle, dataStyle, dataStyle, dataStyle, dataStyle,
                theme.GetProfitStyle(profitToShow), theme.GetProfitStyle(roi),
                theme.GetProfitStyle(s.AvgSoilCompatibility - 50f)
            };

            UIDrawUtils.DrawRow(x, y, colOffsets, values, styles, colWidth: 75f);
        }

        private void DrawOperationalBreakdown(float x, float y, CropStats totals, UITheme theme)
        {
            GUI.Label(new Rect(x, y, 320, 18), "DETALII COSTURI OPERAȚIONALE (LOGISTICĂ)", theme.Value);
            y += 20;
            MapHelper.DrawBox(new Rect(x, y, 300, 1), new Color(1f, 1f, 1f, 0.1f));
            y += 8;
            
            float indent = 12;
            float valueX = x + indent + 180;

            DrawSmallRow(x + indent, valueX, ref y, "Venit Recoltat:", totals.HarvestedRevenue.ToString("F2") + " €", theme.Label, theme.Good);
            DrawSmallRow(x + indent, valueX, ref y, "Venit în Câmp:", totals.FieldRevenue.ToString("F2") + " €", theme.Label, theme.Value);
            y += 4;
            DrawSmallRow(x + indent, valueX, ref y, "Consum Energie:", totals.TotalEnergyCost.ToString("F2") + " €", theme.Label, theme.Value);
            DrawSmallRow(x + indent, valueX, ref y, "Mentenanță Utilitaje:", totals.TotalMaintenanceCost.ToString("F2") + " €", theme.Label, theme.Value);
            DrawSmallRow(x + indent, valueX, ref y, "Amortizare Active:", totals.TotalDepreciationCost.ToString("F2") + " €", theme.Label, theme.Value);
            
            y += 8;
            UIDrawUtils.DrawHorizontalLine(x + indent, y, 420);
            y += 10;

            Color profitBg = totals.NetProfit >= 0 ? new Color(0.1f, 0.8f, 0.3f, 0.08f) : new Color(0.9f, 0.2f, 0.2f, 0.08f);
            MapHelper.DrawBox(new Rect(x, y - 2, 450, 20), profitBg);
            DrawSmallRow(x + indent, valueX, ref y, "PROFIT NET FINAL:", totals.NetProfit.ToString("F2") + " €", theme.Label, theme.GetProfitStyle(totals.NetProfit));
        }

        private void DrawSmallRow(float x, float valueX, ref float y, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUI.Label(new Rect(x, y, 175, 18), label, labelStyle);
            GUI.Label(new Rect(valueX, y, 120, 18), value, valueStyle);
            y += 20;
        }
    }
}
