using UnityEngine;
using System.Collections.Generic;
using Settings.Tabs;

namespace Settings
{
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        bool isOpen;
        private List<ISettingsTab> tabs;
        private int currentTabIndex;
        private CropDatabase cropDB;
        private string[] cropNames;

        void Start() { RefreshData(); }

        void Update() 
        { 
            if (Input.GetKeyDown(KeyCode.P)) 
            {
                isOpen = !isOpen;
                if (isOpen) RefreshData(); 
                else SimulationSettings.OnSettingsChanged?.Invoke();
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

            // Initialize tabs
            tabs = new List<ISettingsTab>
            {
                new GeneralTab(cropNames),
                new EconomicsTab(cropDB),
                new NutrientsTab(cropDB),
                new RobotStatsTab()
            };
        }

        void OnGUI()
        {
            if (!isOpen || theme == null) return;
            
            // Safety check for initialized tabs
            if (tabs == null || tabs.Count == 0) 
            {
                RefreshData();
                if (tabs == null) return;
            }

            float w = 540f, h = 600f;
            Rect p = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            theme.DrawPanel(p);

            float x = p.x + 20, y = p.y + 15;

            // Tabs Header
            DrawTabs(new Rect(x, y, w - 40, 30));
            y += 45;

            // Tab Content
            if (currentTabIndex < tabs.Count)
            {
                Rect contentArea = new Rect(x, y, w - 40, p.yMax - y - 40);
                GUI.BeginGroup(contentArea);
                tabs[currentTabIndex].Draw(new Rect(0, 0, contentArea.width, contentArea.height), theme);
                GUI.EndGroup();
            }

            // Footer
            GUI.Label(new Rect(p.x, p.yMax - 28, w, 20), "Apasă P pentru a închide", theme.Footer);
            
            if (GUI.Button(new Rect(p.x + (w / 2) - 40, p.yMax - 30, 80, 22), "Aplică", theme.Button))
            {
                SimulationSettings.OnSettingsChanged?.Invoke();
            }

            if (GUI.Button(new Rect(p.xMax - 30, p.y + 5, 25, 25), "X", theme.Bad))
            {
                isOpen = false;
                SimulationSettings.OnSettingsChanged?.Invoke();
            }
        }

        private void DrawTabs(Rect area)
        {
            if (tabs == null || tabs.Count == 0) return;

            float tabWidth = area.width / tabs.Count;
            for (int i = 0; i < tabs.Count; i++)
            {
                Rect r = new Rect(area.x + i * tabWidth, area.y, tabWidth - 5, area.height);
                GUI.backgroundColor = (i == currentTabIndex) ? Color.cyan : Color.white;
                if (GUI.Button(r, tabs[i].Title, theme.Button))
                {
                    currentTabIndex = i;
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
