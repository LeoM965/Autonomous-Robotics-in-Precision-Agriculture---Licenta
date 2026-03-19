using UnityEngine;
using Settings.Utils;

namespace Settings.Tabs
{
    public class NutrientsTab : ISettingsTab
    {
        public string Title => "Nutrienți";
        private Vector2 scroll;
        private CropDatabase cropDB;
        private GUIStyle smallBold;

        public NutrientsTab(CropDatabase db) => cropDB = db;

        public void Draw(Rect area, UITheme theme)
        {
            float x = 0, y = 0;
            if (cropDB?.crops != null && SimulationSettings.SeedCosts != null)
            {
                Rect scrollArea = new Rect(x, y, area.width, area.height);
                Rect content = new Rect(0, 0, area.width - 20, cropDB.crops.Length * 110);
                scroll = GUI.BeginScrollView(scrollArea, scroll, content);

                float sy = 0;
                if (smallBold == null)
                    smallBold = new GUIStyle(theme.Label) { fontSize = 10, fontStyle = FontStyle.Bold };
                
                for (int i = 0; i < cropDB.crops.Length; i++)
                {
                    GUI.Label(new Rect(0, sy, 340, 22), $"CULTURĂ: {cropDB.crops[i].name.ToUpper()}", theme.Title);
                    sy += 25;

                    // Compact Nutrients Section with labels
                    GUI.Label(new Rect(0, sy, 50, 18), "AZOT (N)", smallBold);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Min", ref SimulationSettings.N_Min[i], 60, 0, 200, theme);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Opt", ref SimulationSettings.N_Opt[i], 175, 10, 400, theme);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Max", ref SimulationSettings.N_Max[i], 285, 20, 800, theme);
                    sy += 22;

                    GUI.Label(new Rect(0, sy, 50, 18), "FOSFOR (P)", smallBold);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Min", ref SimulationSettings.P_Min[i], 60, 0, 150, theme);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Opt", ref SimulationSettings.P_Opt[i], 175, 5, 300, theme);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Max", ref SimulationSettings.P_Max[i], 285, 10, 600, theme);
                    sy += 22;

                    GUI.Label(new Rect(0, sy, 50, 18), "POTASIU (K)", smallBold);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Min", ref SimulationSettings.K_Min[i], 60, 0, 250, theme);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Opt", ref SimulationSettings.K_Opt[i], 175, 10, 500, theme);
                    SettingsUIHelper.DrawCompactSlider(ref sy, "Max", ref SimulationSettings.K_Max[i], 285, 20, 1000, theme);
                    
                    sy += 35;
                }
                GUI.EndScrollView();
            }
        }
    }
}
