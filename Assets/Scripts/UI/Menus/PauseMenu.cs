using UnityEngine;
using System.Collections.Generic;
using Economics.Models;
using Sensors.Components;

public class PauseMenu : MonoBehaviour
{
    [Header("Theme")]
    [SerializeField] private UITheme theme;
    [Header("Robot Principal")]
    [SerializeField] private string robotId = "AgBot";
    private enum Tab { Simulare, Economie }
    private Tab currentTab = Tab.Simulare;
    private bool isOpen;
    private float simTime;
    private MultiRobotSpawner spawner;
    private RobotStatsTracker tracker;
    private List<EnvironmentalSensor> parcels;
    private UITheme t;
    private float investment;
    private float operatingCost;
    private int productiveParcels;
    private int totalParcels;
    private int robotCount;
    private int robotsPerType;
    private void Start()
    {
        spawner = FindFirstObjectByType<MultiRobotSpawner>();
        tracker = new RobotStatsTracker();
        Invoke(nameof(Init), 0.5f);
    }
    private void Init()
    {
        parcels = new List<EnvironmentalSensor>(ParcelCache.Parcels);
        CalculateInvestment();
    }
    private void CalculateInvestment()
    {
        if (spawner == null) return;
        List<GameObject> bots = spawner.GetRobots();
        if (bots == null || bots.Count == 0) return;
        for (int i = 0; i < bots.Count; i++)
        {
            if (bots[i] == null) continue;
            var robotData = Economics.Services.EconomicDataLoader.GetRobot(robotId);
            if (robotData != null)
                investment += robotData.purchaseCostEUR;
        }
    }
    private void Update()
    {
        if (!isOpen)
        {
            simTime += Time.deltaTime;
            TrackRobots();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }
    private void TrackRobots()
    {
        if (spawner == null) return;
        if (tracker == null) return;
        List<GameObject> bots = spawner.GetRobots();
        tracker.Track(bots, Time.deltaTime);
        operatingCost = tracker.TotalCost;
    }
    private void TogglePause()
    {
        isOpen = !isOpen;
        if (isOpen)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
        if (isOpen) Refresh();
    }
    private void Refresh()
    {
        if (parcels == null || spawner == null) return;
        List<GameObject> bots = spawner.GetRobots();
        if (bots != null)
            robotCount = bots.Count;
        else
            robotCount = 0;
        robotsPerType = spawner.RobotsPerType;
        totalParcels = parcels.Count;
        productiveParcels = 0;
        for (int i = 0; i < parcels.Count; i++)
        {
            EnvironmentalSensor p = parcels[i];
            if (p != null && p.composition != null && p.LatestAnalysis.qualityScore >= 65f)
                productiveParcels++;
        }
        if (tracker != null)
        {
            tracker.Track(bots, 0.1f);
            operatingCost = tracker.TotalCost;
        }
    }
    private void OnGUI()
    {
        if (!isOpen) return;
        if (theme != null)
            t = theme;
        else
            t = UITheme.Default;
        float w = 460f;
        float h = 340f;
        Rect panel = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        t.DrawPanel(panel);
        DrawContent(panel);
    }
    private void DrawContent(Rect panel)
    {
        float x = panel.x + 25;
        float y = panel.y + 20;
        GUI.Label(new Rect(x, y, panel.width - 50, 28), "ANALIZA FERMEI", t.Header);
        y += 40;
        DrawTabs(x, y, panel.width - 60);
        y += 45;
        switch (currentTab)
        {
            case Tab.Simulare: DrawSimulare(x, ref y); break;
            case Tab.Economie: DrawEconomie(x, ref y); break;
        }
        GUI.Label(new Rect(panel.x, panel.yMax - 28, panel.width, 20),
            "Apasa ESC pentru a continua", t.Footer);
    }
    private void DrawTabs(float x, float y, float totalWidth)
    {
        float tw = totalWidth / 2f;
        DrawTab(new Rect(x, y, tw - 5, 28), "Simulare", Tab.Simulare);
        DrawTab(new Rect(x + tw, y, tw - 5, 28), "Economie", Tab.Economie);
    }
    private void DrawTab(Rect rect, string text, Tab tab)
    {
        bool active = currentTab == tab;
        if (active)
            GUI.DrawTexture(rect, t.TabActiveBg);
        else
            GUI.DrawTexture(rect, t.TabBg);
        GUIStyle style = active ? t.TabActive : t.Tab;
        if (GUI.Button(rect, text, style))
            currentTab = tab;
    }
    private void DrawSimulare(float x, ref float y)
    {
        float pct = 0f;
        if (totalParcels > 0)
            pct = productiveParcels * 100f / totalParcels;
        float distance = 0f;
        if (tracker != null)
            distance = tracker.TotalDistance;
        Row(x, ref y, "Timp Rulare", TimeHelper.FormatHours(simTime), null);
        Row(x, ref y, "Roboti Activi", robotCount.ToString(), null);
        Row(x, ref y, "Total Parcele", totalParcels.ToString(), null);
        Row(x, ref y, "Productive", productiveParcels + " (" + pct.ToString("F0") + "%)", t.GetQualityStyle(pct));
        Row(x, ref y, "Distanta", distance.ToString("F0") + " m", null);
    }
    private void DrawEconomie(float x, ref float y)
    {
        // Simultaneous 3-Robot View
        float columnWidth = (t.Header.fixedWidth > 0 ? t.Header.fixedWidth : 135f);
        float spacing = 10f;
        
        string[] robotsToCompare = { "AgBot", "FarmBot", "AgroBot" };
        
        y -= 5;
        // Icons/Headers
        for (int i = 0; i < robotsToCompare.Length; i++)
        {
            Rect headerRect = new Rect(x + i * (columnWidth + spacing), y, columnWidth, 24);
            GUI.Label(headerRect, robotsToCompare[i], t.Header);
        }
        y += 30;

        float startY = y;
        for (int i = 0; i < robotsToCompare.Length; i++)
        {
            float currentY = startY;
            DrawRobotColumn(x + i * (columnWidth + spacing), ref currentY, robotsToCompare[i], columnWidth);
            if (i == 0) y = currentY; // Track height for footer
        }
    }

    private void DrawRobotColumn(float x, ref float y, string id, float width)
    {
        FinancialReport r = Economics.Services.FinancialEngine.GenerateReport(id, robotsPerType);
        if (!r.isValid)
        {
            GUI.Label(new Rect(x, y, width, 22), "N/A", t.Bad);
            return;
        }

        DrawMiniRow(x, ref y, "Investiție (CapEx)", r.totalInvestment.ToString("N0") + " €", width, null);
        DrawMiniRow(x, ref y, "Costuri/An (OpEx)", r.annualOperatingCost.ToString("N0") + " €", width, t.Bad);
        DrawMiniRow(x, ref y, "Rentabilitate (ROI)", r.annualROI.ToString("F1") + "%", width, t.GetProfitStyle(r.annualROI));
        DrawMiniRow(x, ref y, "Profit Net (NPV)", r.netPresentValue.ToString("N0") + " €", width, t.GetProfitStyle(r.netPresentValue));
        
        GUIStyle pbStyle = r.IsInvestmentRecovered ? t.Good : t.Bad;
        DrawMiniRow(x, ref y, "Recuperare (Ani)", r.paybackPeriodYears.ToString("F1") + " ani", width, pbStyle);
    }

    private void DrawMiniRow(float x, ref float y, string label, string value, float width, GUIStyle style)
    {
        GUIStyle labelStyle = new GUIStyle(t.Label) { fontSize = 10 };
        GUIStyle valStyle = style != null ? new GUIStyle(style) { fontSize = 10, alignment = TextAnchor.MiddleRight } : new GUIStyle(t.Value) { fontSize = 10, alignment = TextAnchor.MiddleRight };
        
        GUI.Label(new Rect(x, y, width, 18), label, labelStyle);
        y += 15;
        GUI.Label(new Rect(x, y, width, 18), value, valStyle);
        y += 22;
    }

    private void Row(float x, ref float y, string label, string value, GUIStyle style)
    {
        GUI.Label(new Rect(x, y, 160, 22), label, t.Label);
        GUIStyle valueStyle = style;
        if (valueStyle == null)
            valueStyle = t.Value;
        GUI.Label(new Rect(x + 165, y, 220, 22), value, valueStyle);
        y += 24;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
