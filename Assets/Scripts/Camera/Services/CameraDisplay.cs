using UnityEngine;

public class CameraDisplay : MonoBehaviour
{
    private GUIStyle labelStyle;
    private GUIStyle shadowStyle;

    public void DrawOverlay(CameraMode mode, string targetName, float distance)
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
            labelStyle.normal.textColor = Color.white;
            shadowStyle = new GUIStyle(labelStyle);
            shadowStyle.normal.textColor = new Color(0, 0, 0, 0.5f);
        }

        string info = $"{mode} | {targetName} | {distance:F0}m\n[C] Mode [V] Target [R] Reset";
        GUI.Label(new Rect(11, 11, 500, 50), info, shadowStyle);
        GUI.Label(new Rect(10, 10, 500, 50), info, labelStyle);
    }
}
