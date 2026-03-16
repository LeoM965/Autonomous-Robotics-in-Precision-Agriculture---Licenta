using UnityEngine;

namespace Settings
{
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        bool isOpen;
        CropDatabase cropDB;
        string[] cropNames;
        Vector2 scroll;

        void Start() { RefreshData(); }

        void Update() 
        { 
            if (Input.GetKeyDown(KeyCode.S)) 
            {
                isOpen = !isOpen;
                if (isOpen) RefreshData(); 
            }
        }

        void RefreshData()
        {
            cropDB = CropLoader.Load();
            SimulationSettings.InitFromDatabase(cropDB);
            int count = cropDB.crops.Length;
            cropNames = new string[count + 1];
            cropNames[0] = "Auto";
            for (int i = 0; i < count; i++) cropNames[i + 1] = cropDB.crops[i].name;
            
            // Trigger a re-initialization of SimulationSettings to ensure arrays match new DB length
            SimulationSettings.InitFromDatabase(cropDB);
        }

        void OnGUI()
        {
            if (!isOpen || theme == null) return;
            float w = 420f, h = 540f;
            Rect p = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            theme.DrawPanel(p);

            float x = p.x + 20, y = p.y + 15;

            // Header
            GUI.Label(new Rect(x, y, 380, 30), "CONFIGURARE PLANTE", theme.Header);
            y += 40;

            // Plante per rand
            GUI.Label(new Rect(x, y, 130, 20), "Plante per rând:", theme.Label);
            SimulationSettings.PlantsPerRow = (int)GUI.HorizontalSlider(new Rect(x + 130, y + 5, 110, 20), SimulationSettings.PlantsPerRow, 1, 25);
            string pprInput = GUI.TextField(new Rect(x + 250, y, 30, 20), SimulationSettings.PlantsPerRow.ToString(), theme.Input);
            if (int.TryParse(pprInput, out int pprResult)) SimulationSettings.PlantsPerRow = Mathf.Clamp(pprResult, 1, 25);
            y += 30;

            // Tip cultura
            int idx = SimulationSettings.SelectedCropIndex + 1;
            GUI.Label(new Rect(x, y, 130, 20), "Tip cultură:", theme.Label);
            if (GUI.Button(new Rect(x + 140, y, 25, 22), "<", theme.Button)) idx = (idx - 1 + cropNames.Length) % cropNames.Length;
            GUI.Label(new Rect(x + 170, y, 110, 22), cropNames[idx], theme.Value);
            if (GUI.Button(new Rect(x + 285, y, 25, 22), ">", theme.Button)) idx = (idx + 1) % cropNames.Length;
            SimulationSettings.SelectedCropIndex = idx - 1;
            y += 35;


            // Robot Economics section
            GUI.Label(new Rect(x, y, 380, 20), "ECONOMIE ROBOT", theme.Title);
            y += 25;

            GUI.Label(new Rect(x, y, 140, 20), "Preț Energie (€/kWh):", theme.Label);
            SimulationSettings.EnergyPrice = GUI.HorizontalSlider(new Rect(x + 160, y + 5, 140, 20), SimulationSettings.EnergyPrice, 0.05f, 1.00f);
            GUI.Label(new Rect(x + 310, y, 60, 20), SimulationSettings.EnergyPrice.ToString("F2"), theme.Value);
            y += 40;

            // Separator
            GUI.Label(new Rect(x, y, 380, 20), "PREȚURI PER CULTURĂ", theme.Title);
            y += 25;

            // Definition for small font style
            GUIStyle small = new GUIStyle(theme.Label) { fontSize = 10 };

            // Crop list with scroll view
            if (cropDB?.crops != null && SimulationSettings.SeedCosts != null)
            {
                Rect scrollArea = new Rect(x, y, 380, p.yMax - y - 55);
                Rect content = new Rect(0, 0, 360, cropDB.crops.Length * 210); // Adjusted height for multi-row items
                scroll = GUI.BeginScrollView(scrollArea, scroll, content);

                float sy = 0;
                for (int i = 0; i < cropDB.crops.Length; i++)
                {
                    // Row for the crop name
                    GUI.Label(new Rect(0, sy, 350, 20), cropDB.crops[i].name, theme.Title);
                    sy += 20;

                    // Row for Seed Cost
                    DrawLabeledSlider(ref sy, "Sămânță (€):", ref SimulationSettings.SeedCosts[i], 0.001f, 1.0f, "F3");
                    
                    // Row for Weight
                    DrawLabeledSlider(ref sy, "Rezultat (kg):", ref SimulationSettings.YieldWeights[i], 0.01f, 25f, "F2");

                    // Row for Price
                    DrawLabeledSlider(ref sy, "Preț/kg (€):", ref SimulationSettings.MarketPrices[i], 0.1f, 5f, "F2");
                    sy += 3;

                    // Nutrients Requirements Section (Compact)
                    GUI.Label(new Rect(0, sy, 350, 18), "Cerinte Nutrienti (kg/ha):", small);
                    sy += 18;

                    // Nitrogen N
                    DrawCompactSlider(ref sy, "N Min", ref SimulationSettings.N_Min[i], 0, 0, 200);
                    DrawCompactSlider(ref sy, "Opt", ref SimulationSettings.N_Opt[i], 115, 10, 400);
                    DrawCompactSlider(ref sy, "Max", ref SimulationSettings.N_Max[i], 225, 20, 800);
                    sy += 20;

                    // Phosphorus P
                    DrawCompactSlider(ref sy, "P Min", ref SimulationSettings.P_Min[i], 0, 0, 150);
                    DrawCompactSlider(ref sy, "Opt", ref SimulationSettings.P_Opt[i], 115, 5, 300);
                    DrawCompactSlider(ref sy, "Max", ref SimulationSettings.P_Max[i], 225, 10, 600);
                    sy += 20;

                    // Potassium K
                    DrawCompactSlider(ref sy, "K Min", ref SimulationSettings.K_Min[i], 0, 0, 250);
                    DrawCompactSlider(ref sy, "Opt", ref SimulationSettings.K_Opt[i], 115, 10, 500);
                    DrawCompactSlider(ref sy, "Max", ref SimulationSettings.K_Max[i], 225, 20, 1000);
                    
                    sy += 40; // Spacing between crops
                }
                GUI.EndScrollView();
            }

            // Footer
            GUI.Label(new Rect(p.x, p.yMax - 28, w, 20), "Apasă S pentru a închide", theme.Footer);
            if (GUI.Button(new Rect(p.xMax - 30, p.y + 5, 25, 25), "X", theme.Bad)) isOpen = false;
        }

        private void DrawLabeledSlider(ref float sy, string label, ref float value, float min, float max, string format)
        {
            GUIStyle small = new GUIStyle(theme.Label) { fontSize = 11 }; // Fixed: using theme.Label instead of theme.Small
            GUI.Label(new Rect(0, sy, 80, 20), label, small);
            value = GUI.HorizontalSlider(new Rect(85, sy + 5, 200, 20), value, min, max);
            
            string input = GUI.TextField(new Rect(290, sy, 60, 20), value.ToString(format), theme.Input);
            if (float.TryParse(input, out float result)) value = Mathf.Clamp(result, min, max);
            
            sy += 22;
        }

        private void DrawCompactSlider(ref float sy, string label, ref float value, float offsetX, float min, float max)
        {
            GUIStyle small = new GUIStyle(theme.Label) { fontSize = 10 }; // Fixed: using theme.Label instead of theme.Small
            GUI.Label(new Rect(offsetX, sy, 35, 18), label, small);
            value = GUI.HorizontalSlider(new Rect(offsetX + 35, sy + 4, 45, 18), value, min, max);
            
            string input = GUI.TextField(new Rect(offsetX + 82, sy, 30, 18), value.ToString("F0"), theme.Input);
            if (float.TryParse(input, out float result)) value = Mathf.Clamp(result, min, max);
        }
    }
}
