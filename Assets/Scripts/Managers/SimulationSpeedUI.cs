using UnityEngine;

public class SimulationSpeedUI : MonoBehaviour
{
    private SimulationSpeedController controller;

    private void Start()
    {
        controller = SimulationSpeedController.Instance;
    }

    private void OnGUI()
    {
        if (controller == null) return;

        GUILayout.BeginArea(new Rect(10, 65, 260, 240));
        GUILayout.BeginVertical("box");

        DrawSpeedButtons();
        DrawBoostButton();
        GUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;

        GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            alignment = TextAnchor.MiddleCenter
        };

        string statusText = controller.IsSkipping ? "SKIPPING..." : (Time.timeScale > 0 ? "Speed: " + Time.timeScale + "x" : "PAUSED");
        GUILayout.Label("<b>" + statusText + "</b>", statusStyle);
        GUILayout.Space(5);

        DrawWeatherAndSkipRow();
        
        GUILayout.Space(10);
        DrawMonthJumper();

        GUI.enabled = true;
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawSpeedButtons()
    {
        GUILayout.BeginHorizontal();
        float[] speeds = controller.Speeds;
        for (int i = 0; i < speeds.Length; i++)
        {
            GUI.backgroundColor = controller.CurrentIndex == i ? Color.green : Color.white;
            if (GUILayout.Button(GetLabel(speeds[i]), GUILayout.Width(35), GUILayout.Height(25)))
                controller.SetSpeed(i);
        }
    }

    private void DrawBoostButton()
    {
        GUI.backgroundColor = controller.IsBoostActive ? Color.cyan : Color.white;
        if (GUILayout.Button("x" + controller.BoostMultiplier, GUILayout.Width(45), GUILayout.Height(25)))
            controller.ToggleBoost();
    }

    private void DrawWeatherAndSkipRow()
    {
        GUILayout.BeginHorizontal();
        if (Weather.Components.WeatherSystem.Instance != null)
        {
            var weatherSys = Weather.Components.WeatherSystem.Instance;
            string label = weatherSys.ForcedWeather.HasValue ? weatherSys.ForcedWeather.Value.ToString() : "Auto W.";

            GUI.enabled = !controller.IsSkipping;
            if (GUILayout.Button(label, GUILayout.Height(25), GUILayout.Width(80)))
                weatherSys.CycleForcedWeather();
            GUI.enabled = true;
        }

        GUI.enabled = !controller.IsSkipping;
        if (GUILayout.Button(controller.IsSkipping ? "..." : "Next Day", GUILayout.Height(25)))
            controller.SkipDay();
        GUILayout.EndHorizontal();
    }

    private void DrawMonthJumper()
    {
        if (TimeManager.Instance == null) return;

        GUILayout.Label("<b>Sari la Luna:</b>", 
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        
        string[] months = { "Ian", "Feb", "Mar", "Apr", "Mai", "Iun", "Iul", "Aug", "Sep", "Oct", "Noi", "Dec" };
        GUI.enabled = !controller.IsSkipping;

        for (int i = 0; i < 12; i++)
        {
            if (i % 4 == 0) GUILayout.BeginHorizontal();
            
            if (GUILayout.Button(months[i], GUILayout.Width(55), GUILayout.Height(22)))
            {
                TimeManager.Instance.SkipToDate(1, i + 1);
            }
            
            if (i % 4 == 3) GUILayout.EndHorizontal();
        }
        GUI.enabled = true;
    }

    private string GetLabel(float val)
    {
        if (val == 0) return "||";
        if (val == 1) return ">";
        return val.ToString();
    }
}
