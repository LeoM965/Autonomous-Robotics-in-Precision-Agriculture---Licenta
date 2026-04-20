#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UI.Canvas; // Use the internal Canvas Framework

public class BakeUI_ToEditor
{
    [MenuItem("Farm/Bake Final UI To Hierarchy (Generare In Editor)")]
    public static void BakeUI()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Eroare: Nu exista niciun Canvas activ in scena! Fa un Canvas mai intai.");
            return;
        }

        GameObject uiPanel = CanvasHelper.AddPanel(
            canvas.transform, 
            "SimulationSpeedPanel", 
            new Vector2(0, 1), 
            new Vector2(10, -65), 
            new Vector2(280, 290)
        );

        Undo.RegisterCreatedObjectUndo(uiPanel, "Bake Simulation UI");

        SimulationSpeedUI comp = uiPanel.AddComponent<SimulationSpeedUI>();
        
        float py = -10f;
        CanvasHelper.AddTitle(uiPanel.transform, "CONTROL TIMP", ref py);
        py -= 5f;

        // Speeds array fix (assuming standard 0,1,2,5,10)
        float[] speeds = new float[] { 0, 1, 2, 5, 10 };
        comp.speedButtons = new Button[speeds.Length];
        comp.speedImages = new Image[speeds.Length];

        float currentX = 15f;
        Color inactiveColor = new Color(0.12f, 0.20f, 0.35f, 1f);

        for (int i = 0; i < speeds.Length; i++)
        {
            Button btn = CanvasHelper.AddButton(uiPanel.transform, "Speed_" + i, GetLabel(speeds[i]), new Vector2(currentX, py), new Vector2(35, 25), inactiveColor);
            comp.speedButtons[i] = btn;
            comp.speedImages[i] = btn.targetGraphic as Image;
            // Force texts to be white by default
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = Color.white;
            
            currentX += 38f;
        }

        currentX += 5f;
        comp.boostBtn = CanvasHelper.AddButton(uiPanel.transform, "Boost", "x2", new Vector2(currentX, py), new Vector2(45, 25), inactiveColor);
        comp.boostImage = comp.boostBtn.targetGraphic as Image;
        comp.boostText = comp.boostBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (comp.boostText != null) comp.boostText.color = Color.white;

        py -= 35f;

        comp.statusLabel = CanvasHelper.AddText(uiPanel.transform, "Speed: 1x", CanvasHelper.MainText, 14, FontStyles.Bold, new Vector2(0, py), new Vector2(280, 20), "StatusLabel", TextAlignmentOptions.Center);

        py -= 30f;

        comp.weatherBtn = CanvasHelper.AddButton(uiPanel.transform, "Weather", "Auto W.", new Vector2(30, py), new Vector2(100, 28), inactiveColor);
        comp.weatherText = comp.weatherBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (comp.weatherText != null) comp.weatherText.color = Color.white;

        comp.skipBtn = CanvasHelper.AddButton(uiPanel.transform, "Skip", "Next Day", new Vector2(150, py), new Vector2(100, 28), inactiveColor);
        comp.skipText = comp.skipBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (comp.skipText != null) comp.skipText.color = Color.white;

        py -= 40f;

        CanvasHelper.AddSeparator(uiPanel.transform, ref py);
        CanvasHelper.AddText(uiPanel.transform, "SĂRI LA LUNĂ", CanvasHelper.Accent, 12, FontStyles.Bold, new Vector2(0, py), new Vector2(280, 20), "MonthHeader", TextAlignmentOptions.Center);
        py -= 25f;

        string[] months = { "Ian", "Feb", "Mar", "Apr", "Mai", "Iun", "Iul", "Aug", "Sep", "Oct", "Noi", "Dec" };
        float startX = 22.5f;
        comp.monthButtons = new Button[12];
        for (int i = 0; i < 12; i++)
        {
            int row = i / 4;
            int col = i % 4;
            Button mBtn = CanvasHelper.AddButton(uiPanel.transform, "Month_" + i, months[i], new Vector2(startX + col * 60, py - row * 30), new Vector2(55, 25), inactiveColor);
            var mtxt = mBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (mtxt != null) mtxt.color = Color.white;
            
            comp.monthButtons[i] = mBtn;
        }

        EditorUtility.SetDirty(uiPanel);
        Debug.Log("UI generat cu succes direct in Editor (Ierarhie)! Acum ierarhia contine GameObject-uri fizice, nu se mai genereaza nimic din cod!");
    }

    private static string GetLabel(float val)
    {
        if (val == 0) return "||";
        if (val == 1) return ">";
        return val.ToString();
    }
}
#endif
