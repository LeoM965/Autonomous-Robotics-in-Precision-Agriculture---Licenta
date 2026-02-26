using UnityEngine;
using System.Collections.Generic;

public class RobotInfoPanel : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] UITheme theme;
    
    Transform selected;
    Camera cam;
    MultiRobotSpawner spawner;
    RobotStatsTracker tracker;
    UITheme t;
    bool show = true;
    bool showDecision = true;
    
    void Start()
    {
        cam = Camera.main;
        spawner = FindFirstObjectByType<MultiRobotSpawner>();
        tracker = new RobotStatsTracker();
        
        if (DecisionTracker.Instance == null)
        {
            GameObject trackerGo = new GameObject("DecisionTracker");
            trackerGo.AddComponent<DecisionTracker>();
        }
    }

    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) show = !show;
        if (Input.GetKeyDown(KeyCode.D)) showDecision = !showDecision;
        if (spawner != null && tracker != null)
            tracker.Track(spawner.GetRobots(), Time.deltaTime);
        HandleClick();
    }
    
    void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 500f))
            selected = FindRobot(hit.transform);
    }
    
    Transform FindRobot(Transform tr)
    {
        while (tr != null)
        {
            foreach (var robot in spawner.GetRobots())
                if (robot != null && robot.transform == tr) return tr;
            tr = tr.parent;
        }
        return null;
    }
    
    void OnGUI()
    {
        if (!show || tracker == null || selected == null || !tracker.Has(selected)) return;
        t = theme != null ? theme : UITheme.Default;
        
        var s = tracker.Get(selected);
        string targetInfo = selected.GetComponent<AgroBotFlight>()?.GetStatus() ?? 
                           selected.GetComponent<CropPlanter>()?.GetStatus() ?? 
                           selected.GetComponent<CropHarvester>()?.GetStatus();
        float h = string.IsNullOrEmpty(targetInfo) ? 145 : 165;
        
        // Main Robot Panel
        Rect r = new Rect((Screen.width - 220) / 2, 12, 220, h);
        t.DrawPanel(r);
        float y = r.y + 8, x = r.x + 10;
        GUI.Label(new Rect(x, y, 180, 18), selected.name, t.Title); y += 20;
        Row(x, ref y, "Tip", s.type);
        Row(x, ref y, "Viteza", $"{s.speed:F1} m/s");
        Row(x, ref y, "Distanta", $"{s.distance:F1} m", t.Value);
        Row(x, ref y, "Timp", TimeHelper.FormatSeconds(s.time), t.Value);
        Row(x, ref y, "Pret", $"{s.purchasePrice:F0} EUR");
        Row(x, ref y, "Cost", $"{s.GetOperatingCost():F2} EUR", t.Good);
        if (!string.IsNullOrEmpty(targetInfo))
            Row(x, ref y, "Target", targetInfo, t.Value);
        if (GUI.Button(new Rect(r.xMax - 25, r.y + 5, 20, 20), "X"))
            selected = null;
        
        // Decision Optimization Panel
        if (showDecision && selected != null)
            DrawDecisionPanel(r);
    }
    
    void DrawDecisionPanel(Rect mainPanel)
    {
        DecisionRecord decision = null;
        if (DecisionTracker.Instance != null)
            decision = DecisionTracker.Instance.GetLastDecision(selected);
        
        float panelWidth = 300f;
        float panelHeight = decision != null ? 300f : 60f;
        Rect r = new Rect(mainPanel.xMax + 10, mainPanel.y, panelWidth, panelHeight);
        
        t.DrawPanel(r);
        float y = r.y + 8, x = r.x + 10;
        
        GUI.Label(new Rect(x, y, 200, 18), "OPTIMIZARE DECIZII", t.Title);
        y += 22;
        
        if (decision == null)
        {
            GUI.Label(new Rect(x, y, 220, 16), "Aștept prima decizie...", t.Label);
            return;
        }
        
        // Current decision header
        GUI.Label(new Rect(x, y, 240, 16), $"Parcel: {decision.parcelName}", t.Label);
        y += 18;
        
        // Chosen crop with score
        string chosenText = $"✓ {decision.chosenOption}";
        GUI.Label(new Rect(x, y, 140, 16), chosenText, t.Good);
        GUI.Label(new Rect(x + 145, y, 80, 16), $"{decision.chosenScore:F1} pts", t.Value);
        y += 20;
        
        // Separator
        GUI.Box(new Rect(x, y, panelWidth - 20, 1), "");
        y += 5;
        
        // Alternatives
        GUI.Label(new Rect(x, y, 200, 16), "Alternative evaluate:", t.Label);
        y += 16;
        
        int maxAlternatives = Mathf.Min(5, decision.alternatives.Count);
        for (int i = 0; i < maxAlternatives; i++)
        {
            DecisionAlternative alt = decision.alternatives[i];
            string prefix = alt.isChosen ? "✓" : "  ";
            GUIStyle style = alt.isChosen ? t.Good : t.Label;
            
            GUI.Label(new Rect(x, y, 150, 15), $"{prefix} {alt.name}", style);
            GUI.Label(new Rect(x + 155, y, 60, 15), $"{alt.score:F1}", style);
            
            // Score bar
            float barWidth = (alt.score / 100f) * 40f;
            Color barColor = alt.score >= 70 ? new Color(0.2f, 0.8f, 0.3f) : 
                            alt.score >= 40 ? new Color(0.9f, 0.7f, 0.2f) : 
                            new Color(0.9f, 0.3f, 0.2f);
            DrawBar(new Rect(x + 210, y + 3, 40, 10), alt.score / 100f, barColor);
            
            y += 15;
        }
        
        y += 5;
        GUI.Box(new Rect(x, y, panelWidth - 20, 1), "");
        y += 8;
        
        // Decision factors
        GUI.Label(new Rect(x, y, 200, 16), "Factori decizie:", t.Label);
        y += 16;
        
        DecisionFactors f = decision.factors;
        DrawFactorRow(x, ref y, "pH", f.phScore);
        DrawFactorRow(x, ref y, "Umiditate", f.humidityScore);
        DrawFactorRow(x, ref y, "Azot (N)", f.nitrogenScore);
        DrawFactorRow(x, ref y, "Fosfor (P)", f.phosphorusScore);
        DrawFactorRow(x, ref y, "Potasiu (K)", f.potassiumScore);
        
        // Stats footer
        y += 5;
        int totalDecisions = DecisionTracker.Instance.GetTotalDecisions(selected);
        float avgScore = DecisionTracker.Instance.GetAverageScore(selected);
        GUI.Label(new Rect(x, y, 240, 16), $"Total decizii: {totalDecisions} | Scor mediu: {avgScore:F1}", t.Label);
    }
    
    void DrawFactorRow(float x, ref float y, string label, float score)
    {
        GUI.Label(new Rect(x, y, 80, 14), label, t.Label);
        
        Color barColor = score >= 70 ? new Color(0.2f, 0.8f, 0.3f) : 
                        score >= 40 ? new Color(0.9f, 0.7f, 0.2f) : 
                        new Color(0.9f, 0.3f, 0.2f);
        DrawBar(new Rect(x + 85, y + 2, 100, 10), score / 100f, barColor);
        
        GUI.Label(new Rect(x + 190, y, 40, 14), $"{score:F0}%", t.Value);
        y += 15;
    }
    
    void DrawBar(Rect rect, float fill, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        
        GUI.color = color;
        Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        
        GUI.color = oldColor;
    }
    
    void Row(float x, ref float y, string label, string val, GUIStyle style = null)
    {
        GUI.Label(new Rect(x, y, 70, 16), label, t.Label);
        GUI.Label(new Rect(x + 72, y, 130, 16), val, style != null ? style : t.Label);
        y += 17;
    }
}

