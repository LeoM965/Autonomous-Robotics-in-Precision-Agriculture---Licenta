using UnityEngine;

namespace Settings.Tabs
{
    public class GeneralTab : ISettingsTab
    {
        public string Title => "General";
        private string[] cropNames;

        public GeneralTab(string[] names)
        {
            cropNames = names;
        }

        public void Draw(Rect area, UITheme theme)
        {
            float x = 0, y = 0;
            GUI.Label(new Rect(x, y, 350, 20), "CONFIGURARE SIMULARE", theme.Title);
            y += 35;

            // Plants per row
            GUI.Label(new Rect(x, y, 160, 20), "Plante per rând:", theme.Label);
            SimulationSettings.PlantsPerRow = (int)GUI.HorizontalSlider(new Rect(x + 160, y + 5, 140, 20), SimulationSettings.PlantsPerRow, 1, 25);
            string pprInput = GUI.TextField(new Rect(x + 310, y, 40, 20), SimulationSettings.PlantsPerRow.ToString(), theme.Input);
            if (int.TryParse(pprInput, out int pprResult)) SimulationSettings.PlantsPerRow = Mathf.Clamp(pprResult, 1, 25);
            y += 35;

            // Capacitate Semințe
            GUI.Label(new Rect(x, y, 160, 20), "Capacitate Semințe:", theme.Label);
            SimulationSettings.MaxSeedsCapacity = (int)GUI.HorizontalSlider(new Rect(x + 160, y + 5, 140, 20), SimulationSettings.MaxSeedsCapacity, 10, 500);
            string capInput = GUI.TextField(new Rect(x + 310, y, 40, 20), SimulationSettings.MaxSeedsCapacity.ToString(), theme.Input);
            if (int.TryParse(capInput, out int capResult)) SimulationSettings.MaxSeedsCapacity = Mathf.Clamp(capResult, 10, 500);
            y += 35;

            // Crop type
            if (cropNames != null && cropNames.Length > 0)
            {
                int idx = SimulationSettings.SelectedCropIndex + 1;
                GUI.Label(new Rect(x, y, 160, 20), "Tip cultură:", theme.Label);
                if (GUI.Button(new Rect(x + 160, y, 25, 22), "<", theme.Button)) idx = (idx - 1 + cropNames.Length) % cropNames.Length;
                GUI.Label(new Rect(x + 190, y, 120, 22), cropNames[idx], theme.Value);
                if (GUI.Button(new Rect(x + 315, y, 25, 22), ">", theme.Button)) idx = (idx + 1) % cropNames.Length;
                SimulationSettings.SelectedCropIndex = idx - 1;
                y += 35;
            }

            // Simulation Mode Cycle
            GUI.Label(new Rect(x, y, 160, 20), "Mod Planificare:", theme.Label);
            if (GUI.Button(new Rect(x + 160, y, 25, 22), "<", theme.Button)) 
                SimulationSettings.UseCentralizedScheduling = !SimulationSettings.UseCentralizedScheduling;
            
            string modeName = SimulationSettings.UseCentralizedScheduling ? "TaskManager" : "Secvențial";
            GUI.Label(new Rect(x + 190, y, 120, 22), modeName, theme.Value);
            
            if (GUI.Button(new Rect(x + 315, y, 25, 22), ">", theme.Button)) 
                SimulationSettings.UseCentralizedScheduling = !SimulationSettings.UseCentralizedScheduling;
            y += 35;

            // Robot count grid
            DrawRobotCountGrid(x, y, theme);
        }

        private void DrawRobotCountGrid(float x, float y, UITheme theme)
        {
            int types = SimulationSettings.RobotTypeCount;
            int zones = SimulationSettings.ZoneCount;
            string[] typeNames = SimulationSettings.RobotTypeNames;
            if (types == 0 || zones == 0 || SimulationSettings.RobotCounts == null) return;

            GUI.Label(new Rect(x, y, 350, 20), "ROBOȚI PER ZONĂ", theme.Title);
            y += 25;

            float colLabel = 100f;
            float colWidth = 85f;
            float rowHeight = 24f;

            // Header row: zone labels
            for (int z = 0; z < zones; z++)
            {
                string zoneName = ((char)('A' + z)).ToString();
                GUI.Label(new Rect(x + colLabel + z * colWidth, y, colWidth, rowHeight), $"Zona {zoneName}", theme.Value);
            }
            y += rowHeight;

            // One row per robot type
            for (int t = 0; t < types; t++)
            {
                string label = (typeNames != null && t < typeNames.Length) ? typeNames[t] : $"Tip {t}";
                GUI.Label(new Rect(x, y, colLabel - 5, rowHeight), label, theme.Label);

                for (int z = 0; z < zones; z++)
                {
                    float cx = x + colLabel + z * colWidth;
                    int count = SimulationSettings.GetCountForTypeZone(t, z);

                    if (GUI.Button(new Rect(cx, y, 20, 20), "-", theme.Button))
                        SimulationSettings.SetCountForTypeZone(t, z, Mathf.Max(0, count - 1));

                    GUI.Label(new Rect(cx + 25, y, 25, 20), count.ToString(), theme.Value);

                    if (GUI.Button(new Rect(cx + 50, y, 20, 20), "+", theme.Button))
                        SimulationSettings.SetCountForTypeZone(t, z, Mathf.Min(10, count + 1));
                }
                y += rowHeight + 4;
            }
        }
    }
}
