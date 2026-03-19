using UnityEngine;
using Economics.Services;
using Economics.Models;

namespace UI.Menus
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Aspect Vizual")]
        [SerializeField] private UITheme theme;
        
        public bool IsOpen { get; private set; }
        
        private enum DashboardTab { Crops, Robots, History }
        private DashboardTab currentTab = DashboardTab.Crops;
        
        private Tabs.CropDashboardTab cropTab;
        private Tabs.RobotDashboardTab robotTab;
        private Tabs.HistoryDashboardTab historyTab;

        private CropDatabase cachedDB;
        private EconomicReport activeReport;
        private int cachedRobotCount;

        private void Awake()
        {
            cropTab = new Tabs.CropDashboardTab();
            robotTab = new Tabs.RobotDashboardTab();
            historyTab = new Tabs.HistoryDashboardTab();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        private void TogglePause()
        {
            IsOpen = !IsOpen;
            
            if (SimulationSpeedController.Instance != null)
                SimulationSpeedController.Instance.SetPaused(IsOpen);
            else
                Time.timeScale = IsOpen ? 0f : 1f;

            if (IsOpen)
            {
                cachedDB = CropLoader.Load();
                activeReport = CropEconomicsCalculator.GetAnalysis(cachedDB);
                robotTab.CacheRobotData();
                cachedRobotCount = Economics.Managers.RobotEconomicsManager.Instance != null 
                    ? Economics.Managers.RobotEconomicsManager.Instance.RobotStatsMap.Count : 0;
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || cachedDB?.crops == null || activeReport.AnalysisByVariety == null) return;
            DrawAnalysisDashboard();
        }

        private void DrawAnalysisDashboard()
        {
            float width = 650f;
            float height = Mathf.Min(Screen.height * 0.85f, 80f + (cachedDB.crops.Length * 17f) + 240f);
            if (currentTab == DashboardTab.Robots)
                height = Mathf.Min(Screen.height * 0.85f, 130f + cachedRobotCount * 18f + 100f);
            Rect panel = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);

            theme.DrawPanel(panel);

            float x = panel.x + 20;
            float y = panel.y + 15;

            DrawTabs(panel.x + 20, panel.y + 15);
            y += 40;

            float contentBottom = panel.yMax - 30;

            if (currentTab == DashboardTab.Crops)
                cropTab.DrawTab(x, y, activeReport, cachedDB.crops, theme);
            else if (currentTab == DashboardTab.Robots)
                robotTab.DrawTab(x, y, theme, contentBottom);
            else
                historyTab.DrawTab(x, y, theme);
            
            GUI.Label(new Rect(panel.x, panel.yMax - 25, width, 20), "Apasă ESC pentru a închide raportul", theme.Footer);
        }

        private void DrawTabs(float x, float y)
        {
            float tabWidth = 90;
            if (GUI.Button(new Rect(x, y, tabWidth, 25), "CULTURI", currentTab == DashboardTab.Crops ? theme.Value : theme.Label))
                currentTab = DashboardTab.Crops;
            
            if (GUI.Button(new Rect(x + tabWidth + 5, y, tabWidth, 25), "ROBOȚI", currentTab == DashboardTab.Robots ? theme.Value : theme.Label))
                currentTab = DashboardTab.Robots;

            if (GUI.Button(new Rect(x + (tabWidth + 5) * 2, y, tabWidth, 25), "ISTORIC", currentTab == DashboardTab.History ? theme.Value : theme.Label))
                currentTab = DashboardTab.History;
        }

        private void OnDestroy()
        {
            if (SimulationSpeedController.Instance != null)
                SimulationSpeedController.Instance.SetPaused(false);
            else
                Time.timeScale = 1f;
        }
    }
}
