using UnityEngine;
using Sensors.Components;

public class ParcelInfoPanel : MonoBehaviour
{
    [SerializeField] UITheme theme;
    EnvironmentalSensor selected;
    Camera cam;
    UITheme t;
    void Start() => cam = Camera.main;
    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 500f)) return;
        var sensor = hit.collider.GetComponent<EnvironmentalSensor>();
        if (sensor == null) sensor = hit.collider.GetComponentInParent<EnvironmentalSensor>();
        if (sensor != null) selected = sensor;
    }
    void OnGUI()
    {
        if (selected == null || selected.composition == null) return;
        t = theme != null ? theme : UITheme.Default;
        Rect panel = new Rect(10, Screen.height - 220, 200, 210);
        t.DrawPanel(panel);
        float y = panel.y + 8, x = panel.x + 8;
        float quality = selected.LatestAnalysis.qualityScore;
        GUI.Label(new Rect(x, y, 190, 16), selected.name, t.Title); y += 20;
        Row(x, ref y, "Calitate", $"{quality:F1}%", t.GetQualityStyle(quality));
        Row(x, ref y, "Umiditate", $"{selected.composition.moisture:F1}%");
        Row(x, ref y, "pH", $"{selected.composition.pH:F1}");
        Row(x, ref y, "Azot", $"{selected.composition.nitrogen:F0} kg/ha");
        Row(x, ref y, "Fosfor", $"{selected.composition.phosphorus:F0} kg/ha");
        Row(x, ref y, "Potasiu", $"{selected.composition.potassium:F0} kg/ha");
        int cropCount = selected.activeCrops.Count;
        string cropType = !string.IsNullOrEmpty(selected.plantedVarietyName) ? selected.plantedVarietyName : "-";
        Row(x, ref y, "Plante", $"{cropCount} ({cropType})");
        
        if (cropCount > 0)
        {
            CropGrowth firstPlant = null;
            foreach (var c in selected.activeCrops) { firstPlant = c; break; }
            if (firstPlant != null)
            {
                Row(x, ref y, "Stadiu", firstPlant.CurrentStage.ToString());
                Row(x, ref y, "Progres", $"{(firstPlant.Progress * 100f):F0}%");
            }
        }
    }
    void Row(float x, ref float y, string label, string val, GUIStyle style = null)
    {
        GUI.Label(new Rect(x, y, 80, 16), label, t.Label);
        GUI.Label(new Rect(x + 82, y, 100, 16), val, style != null ? style : t.Label);
        y += 18;
    }
}
