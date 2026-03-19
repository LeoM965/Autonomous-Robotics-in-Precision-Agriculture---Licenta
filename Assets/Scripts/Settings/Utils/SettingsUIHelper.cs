using UnityEngine;

namespace Settings.Utils
{
    public static class SettingsUIHelper
    {
        public static void DrawLabeledSlider(ref float sy, string label, ref float value, float min, float max, string format, UITheme theme)
        {
            GUIStyle small = new GUIStyle(theme.Label) { fontSize = 11 };
            GUI.Label(new Rect(0, sy, 80, 20), label, small);
            value = GUI.HorizontalSlider(new Rect(85, sy + 5, 200, 20), value, min, max);
            
            string input = GUI.TextField(new Rect(290, sy, 60, 20), value.ToString(format), theme.Input);
            if (float.TryParse(input, out float result)) value = Mathf.Clamp(result, min, max);
            
            sy += 22;
        }

        public static void DrawCompactSlider(ref float sy, string label, ref float value, float offsetX, float min, float max, UITheme theme)
        {
            GUIStyle small = new GUIStyle(theme.Label) { fontSize = 10 };
            GUI.Label(new Rect(offsetX, sy, 35, 18), label, small);
            value = GUI.HorizontalSlider(new Rect(offsetX + 35, sy + 4, 45, 18), value, min, max);
            
            string input = GUI.TextField(new Rect(offsetX + 82, sy, 30, 18), value.ToString("F0"), theme.Input);
            if (float.TryParse(input, out float result)) value = Mathf.Clamp(result, min, max);
        }
    }
}
