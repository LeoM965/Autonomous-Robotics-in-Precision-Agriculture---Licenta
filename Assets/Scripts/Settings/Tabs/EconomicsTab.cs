using UnityEngine;
using Settings.Utils;

namespace Settings.Tabs
{
    public class EconomicsTab : ISettingsTab
    {
        public string Title => "Economie";
        private Vector2 scroll;
        private CropDatabase cropDB;

        public EconomicsTab(CropDatabase db) => cropDB = db;

        public void Draw(Rect area, UITheme theme)
        {
            float x = 0, y = 0;

            GUI.Label(new Rect(x, y, 140, 20), "Preț Energie (€/kWh):", theme.Label);
            SimulationSettings.EnergyPrice = GUI.HorizontalSlider(new Rect(x + 160, y + 5, 140, 20), SimulationSettings.EnergyPrice, 0.05f, 1.00f);
            GUI.Label(new Rect(x + 310, y, 60, 20), SimulationSettings.EnergyPrice.ToString("F2"), theme.Value);
            y += 40;

            GUI.Label(new Rect(x, y, 350, 20), "PREȚURI PER CULTURĂ", theme.Title);
            y += 25;

            if (cropDB?.crops != null && SimulationSettings.SeedCosts != null)
            {
                Rect scrollArea = new Rect(x, y, area.width, area.height - y);
                Rect content = new Rect(0, 0, area.width - 20, cropDB.crops.Length * 100);
                scroll = GUI.BeginScrollView(scrollArea, scroll, content);

                float sy = 0;
                foreach (var crop in cropDB.crops)
                {
                    int i = System.Array.IndexOf(cropDB.crops, crop);
                    GUI.Label(new Rect(0, sy, 340, 22), crop.name.ToUpper(), theme.Title);
                    sy += 25;

                    SettingsUIHelper.DrawLabeledSlider(ref sy, "Sămânță (€):", ref SimulationSettings.SeedCosts[i], 0.001f, 1.0f, "F3", theme);
                    SettingsUIHelper.DrawLabeledSlider(ref sy, "Recoltă (kg):", ref SimulationSettings.YieldWeights[i], 0.01f, 10f, "F2", theme);
                    SettingsUIHelper.DrawLabeledSlider(ref sy, "Preț Piață (€):", ref SimulationSettings.MarketPrices[i], 0.1f, 5f, "F2", theme);
                    
                    sy += 20;
                }
                GUI.EndScrollView();
            }
        }
    }
}
