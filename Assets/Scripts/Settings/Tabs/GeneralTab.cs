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
            GUI.Label(new Rect(x, y, 130, 20), "Plante per rând:", theme.Label);
            SimulationSettings.PlantsPerRow = (int)GUI.HorizontalSlider(new Rect(x + 130, y + 5, 110, 20), SimulationSettings.PlantsPerRow, 1, 25);
            string pprInput = GUI.TextField(new Rect(x + 250, y, 30, 20), SimulationSettings.PlantsPerRow.ToString(), theme.Input);
            if (int.TryParse(pprInput, out int pprResult)) SimulationSettings.PlantsPerRow = Mathf.Clamp(pprResult, 1, 25);
            y += 40;

            // Crop type
            if (cropNames != null && cropNames.Length > 0)
            {
                int idx = SimulationSettings.SelectedCropIndex + 1;
                GUI.Label(new Rect(x, y, 130, 20), "Tip cultură:", theme.Label);
                if (GUI.Button(new Rect(x + 140, y, 25, 22), "<", theme.Button)) idx = (idx - 1 + cropNames.Length) % cropNames.Length;
                GUI.Label(new Rect(x + 170, y, 110, 22), cropNames[idx], theme.Value);
                if (GUI.Button(new Rect(x + 285, y, 25, 22), ">", theme.Button)) idx = (idx + 1) % cropNames.Length;
                SimulationSettings.SelectedCropIndex = idx - 1;
                y += 35;
            }

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

            float colLabel = 90f;
            float colWidth = 70f;
            float rowHeight = 24f;

            // Header row: zone labels
            for (int z = 0; z < zones; z++)
            {
                string zoneName = ((char)('A' + z)).ToString();
                GUI.Label(new Rect(x + colLabel + z * colWidth, y, colWidth, rowHeight), zoneName, theme.Value);
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

                    if (GUI.Button(new Rect(cx, y, 18, 20), "-", theme.Button))
                        SimulationSettings.SetCountForTypeZone(t, z, Mathf.Max(0, count - 1));

                    GUI.Label(new Rect(cx + 20, y, 22, 20), count.ToString(), theme.Value);

                    if (GUI.Button(new Rect(cx + 42, y, 18, 20), "+", theme.Button))
                        SimulationSettings.SetCountForTypeZone(t, z, Mathf.Min(10, count + 1));
                }
                y += rowHeight + 4;
            }
        }
    }
}
