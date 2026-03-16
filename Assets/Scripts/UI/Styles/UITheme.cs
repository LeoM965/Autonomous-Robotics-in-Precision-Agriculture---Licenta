using UnityEngine;
using UI.Utils;

[CreateAssetMenu(fileName = "UITheme", menuName = "Farm/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Panels")]
    public Color panelBackground;
    public Color panelBorder;
    public Color tabBackground;
    public Color tabActiveBackground;

    [Header("Text")]
    public Color titleColor;
    public Color labelColor = Color.white;
    public Color valueColor;
    public Color footerColor;
    public Color tabColor;

    [Header("Status")]
    public Color goodColor;
    public Color warnColor;
    public Color badColor;

    private GUIStyle titleStyle, labelStyle, valueStyle, goodStyle, warnStyle, badStyle, headerStyle, footerStyle, tabStyle, tabActiveStyle, buttonStyle;
    private Texture2D tabBackgroundTexture, tabActiveBackgroundTexture;

    private GUIStyle S(ref GUIStyle f, int sz, FontStyle fs, Color c) =>
        f ??= GetOrCreateGuiStyle(sz, fs, c);

    private GUIStyle S(ref GUIStyle f, int sz, FontStyle fs, Color c, TextAnchor a) =>
        f ??= GetOrCreateGuiStyle(sz, fs, c, a);

    private GUIStyle SBtn(ref GUIStyle f, int sz, FontStyle fs, Color c) =>
        f ??= GetOrCreateGuiStyle(sz, fs, c, true);

    public GUIStyle Title => S(ref titleStyle, 13, FontStyle.Bold, titleColor);
    public GUIStyle Header => S(ref headerStyle, 18, FontStyle.Bold, titleColor);
    public GUIStyle Label => S(ref labelStyle, 11, FontStyle.Normal, labelColor);
    public GUIStyle Value => S(ref valueStyle, 11, FontStyle.Bold, valueColor);
    public GUIStyle Good => S(ref goodStyle, 11, FontStyle.Bold, goodColor);
    public GUIStyle Warn => S(ref warnStyle, 11, FontStyle.Bold, warnColor);
    public GUIStyle Bad => S(ref badStyle, 11, FontStyle.Bold, badColor);
    public GUIStyle Footer => S(ref footerStyle, 10, FontStyle.Normal, footerColor, TextAnchor.MiddleCenter);
    public GUIStyle Tab => SBtn(ref tabStyle, 11, FontStyle.Normal, tabColor);
    public GUIStyle TabActive => SBtn(ref tabActiveStyle, 11, FontStyle.Bold, Color.white);
    public GUIStyle Button => SBtn(ref buttonStyle, 12, FontStyle.Bold, Color.white);
    public GUIStyle Input => SBtn(ref labelStyle, 11, FontStyle.Normal, labelColor); // Using a button-like base for border support

    public Texture2D TabBg => tabBackgroundTexture ??= GenerateSolidColorTexture(tabBackground);
    public Texture2D TabActiveBg => tabActiveBackgroundTexture ??= GenerateSolidColorTexture(tabActiveBackground);

    public void DrawPanel(Rect r)
    {
        MapHelper.DrawShadow(r, 3);
        MapHelper.DrawBox(r, panelBackground);
        MapHelper.DrawBorder(r, panelBorder, 2);
    }

    public GUIStyle GetQualityStyle(float p)
    {
        if (p >= 60f) return Good;
        if (p >= 30f) return Warn;
        return Bad;
    }

    public GUIStyle GetProfitStyle(float v) => v >= 0 ? Good : Bad;

    private GUIStyle GetOrCreateGuiStyle(int size, FontStyle f, Color c, bool isBtn = false) =>
        GetOrCreateGuiStyle(size, f, c, TextAnchor.MiddleLeft, isBtn);

    private GUIStyle GetOrCreateGuiStyle(int size, FontStyle f, Color c, TextAnchor a, bool isBtn = false)
    {
        var skin = isBtn ? GUI.skin.button : GUI.skin.label;
        GUIStyle s = new GUIStyle(skin) { fontSize = size, fontStyle = f, alignment = a };
        s.normal.textColor = c;
        if (isBtn) s.normal.background = null;
        return s;
    }

    private Texture2D GenerateSolidColorTexture(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void OnValidate()
    {
        titleStyle = labelStyle = valueStyle = goodStyle = warnStyle = badStyle = headerStyle = footerStyle = null;
        tabStyle = tabActiveStyle = buttonStyle = null;
        tabBackgroundTexture = tabActiveBackgroundTexture = null;
    }
}
