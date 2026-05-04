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
        
        private Tabs.CropDashboardTab cropTab = new Tabs.CropDashboardTab();
        private Tabs.RobotDashboardTab robotTab = new Tabs.RobotDashboardTab();
        private Tabs.HistoryDashboardTab historyTab = new Tabs.HistoryDashboardTab();

        private CropDatabase cachedDB;
        private EconomicReport activeReport;
        private int cachedRobotCount;

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
            
            if (currentTab == DashboardTab.Robots)
                robotTab.CacheRobotData();
            
            MapHelper.DrawBox(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.45f));
            DrawAnalysisDashboard();
        }

        private void DrawAnalysisDashboard()
        {
            float width = 760f;
            float height = Mathf.Min(Screen.height * 0.85f, 120f + (cachedDB.crops.Length * 22f) + 280f);
            if (currentTab == DashboardTab.Robots)
                height = Mathf.Min(Screen.height * 0.85f, 170f + cachedRobotCount * 22f + 140f + robotTab.ExtraHeight);
            Rect panel = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);

            theme.DrawPanel(panel);

            float x = panel.x + 25;
            float y = panel.y + 15;

            GUI.Label(new Rect(x, y, width - 50, 22), "RAPORT ECONOMIC FERMĂ", theme.Header);
            y += 28;
            MapHelper.DrawBox(new Rect(x, y, width - 50, 2), theme.panelBorder);
            y += 12;

            DrawTabs(x, y, width - 50);
            y += 38;

            float contentBottom = panel.yMax - 30;

            if (currentTab == DashboardTab.Crops)
                cropTab.DrawTab(x, y, activeReport, cachedDB.crops, theme);
            else if (currentTab == DashboardTab.Robots)
                robotTab.DrawTab(x, y, theme, contentBottom);
            else
                historyTab.DrawTab(x, y, theme);
            
            GUI.Label(new Rect(panel.x, panel.yMax - 25, width, 20), "Apasă ESC pentru a închide raportul", theme.Footer);
        }

        private void DrawTabs(float x, float y, float totalWidth)
        {
            float tabWidth = 120;
            float tabHeight = 26;
            float gap = 2;
            string[] labels = { "CULTURI", "ROBOȚI", "ISTORIC" };
            DashboardTab[] tabs = { DashboardTab.Crops, DashboardTab.Robots, DashboardTab.History };

            for (int i = 0; i < 3; i++)
            {
                Rect tabRect = new Rect(x + i * (tabWidth + gap), y, tabWidth, tabHeight);
                bool isActive = currentTab == tabs[i];
                
                Color tabBg = isActive ? new Color(theme.panelBorder.r, theme.panelBorder.g, theme.panelBorder.b, 0.3f) 
                                       : new Color(1f, 1f, 1f, 0.04f);
                MapHelper.DrawBox(tabRect, tabBg);
                
                if (isActive)
                    MapHelper.DrawBox(new Rect(tabRect.x, tabRect.yMax - 2, tabRect.width, 2), theme.panelBorder);
                
                if (GUI.Button(tabRect, labels[i], isActive ? theme.Value : theme.Label))
                    currentTab = tabs[i];
            }
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
