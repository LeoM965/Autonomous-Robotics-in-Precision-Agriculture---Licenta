using UnityEngine;
[CreateAssetMenu(fileName = "UITheme", menuName = "Farm/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Panel Colors")]
    public Color panelBackground = new Color(0.08f, 0.08f, 0.12f, 0.92f);
    public Color panelBorder = new Color(0.3f, 0.7f, 1f, 0.8f);
    public Color tabBackground = new Color(0.12f, 0.14f, 0.2f, 1f);
    public Color tabActiveBackground = new Color(0.2f, 0.5f, 0.7f, 1f);
    [Header("Text Colors")]
    public Color titleColor = new Color(0.4f, 0.85f, 1f);
    public Color labelColor = Color.white;
    public Color valueColor = new Color(1f, 0.9f, 0.4f);
    public Color footerColor = new Color(0.5f, 0.5f, 0.6f);
    public Color tabColor = new Color(0.7f, 0.7f, 0.8f);
    [Header("Status Colors")]
    public Color goodColor = new Color(0.4f, 1f, 0.5f);
    public Color warnColor = new Color(1f, 0.85f, 0.3f);
    public Color badColor = new Color(1f, 0.4f, 0.4f);
    private static UITheme _default;
    public static UITheme Default
    {
        get
        {
            if (_default == null)
                _default = CreateInstance<UITheme>();
            return _default;
        }
    }
    private GUIStyle _title, _label, _value, _good, _warn, _bad, _header;
    private GUIStyle _tab, _tabActive, _button, _footer;
    private Texture2D _tabBg, _tabActiveBg;
    public GUIStyle Title
    {
        get
        {
            if (_title == null) _title = CreateStyle(13, FontStyle.Bold, titleColor);
            return _title;
        }
    }
    public GUIStyle Header
    {
        get
        {
            if (_header == null) _header = CreateStyle(18, FontStyle.Bold, titleColor);
            return _header;
        }
    }
    public GUIStyle Label
    {
        get
        {
            if (_label == null) _label = CreateStyle(11, FontStyle.Normal, labelColor);
            return _label;
        }
    }
    public GUIStyle Value
    {
        get
        {
            if (_value == null) _value = CreateStyle(11, FontStyle.Bold, valueColor);
            return _value;
        }
    }
    public GUIStyle Good
    {
        get
        {
            if (_good == null) _good = CreateStyle(11, FontStyle.Bold, goodColor);
            return _good;
        }
    }
    public GUIStyle Warn
    {
        get
        {
            if (_warn == null) _warn = CreateStyle(11, FontStyle.Bold, warnColor);
            return _warn;
        }
    }
    public GUIStyle Bad
    {
        get
        {
            if (_bad == null) _bad = CreateStyle(11, FontStyle.Bold, badColor);
            return _bad;
        }
    }
    public GUIStyle Footer
    {
        get
        {
            if (_footer == null) _footer = CreateStyle(10, FontStyle.Normal, footerColor, TextAnchor.MiddleCenter);
            return _footer;
        }
    }
    public GUIStyle Tab
    {
        get
        {
            if (_tab == null) _tab = CreateButtonStyle(11, FontStyle.Normal, tabColor);
            return _tab;
        }
    }
    public GUIStyle TabActive
    {
        get
        {
            if (_tabActive == null) _tabActive = CreateButtonStyle(11, FontStyle.Bold, Color.white);
            return _tabActive;
        }
    }
    public GUIStyle Button
    {
        get
        {
            if (_button == null) _button = CreateButtonStyle(12, FontStyle.Bold, Color.white);
            return _button;
        }
    }
    public Texture2D TabBg
    {
        get
        {
            if (_tabBg == null) _tabBg = CreateTexture(tabBackground);
            return _tabBg;
        }
    }
    public Texture2D TabActiveBg
    {
        get
        {
            if (_tabActiveBg == null) _tabActiveBg = CreateTexture(tabActiveBackground);
            return _tabActiveBg;
        }
    }
    public void DrawPanel(Rect rect)
    {
        MapHelper.DrawShadow(rect, 3);
        MapHelper.DrawBox(rect, panelBackground);
        MapHelper.DrawBorder(rect, panelBorder, 2);
    }
    public GUIStyle GetQualityStyle(float percent)
    {
        if (percent >= 60f) return Good;
        if (percent >= 30f) return Warn;
        return Bad;
    }
    public GUIStyle GetProfitStyle(float value)
    {
        if (value >= 0) return Good;
        return Bad;
    }
    private GUIStyle CreateStyle(int size, FontStyle font, Color color, TextAnchor align = TextAnchor.MiddleLeft)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = size;
        s.fontStyle = font;
        s.alignment = align;
        s.normal.textColor = color;
        return s;
    }
    private GUIStyle CreateButtonStyle(int size, FontStyle font, Color color)
    {
        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fontSize = size;
        s.fontStyle = font;
        s.normal.textColor = color;
        s.normal.background = null;
        return s;
    }
    private Texture2D CreateTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
    private void OnValidate()
    {
        _title = null;
        _label = null;
        _value = null;
        _good = null;
        _warn = null;
        _bad = null;
        _header = null;
        _tab = null;
        _tabActive = null;
        _button = null;
        _footer = null;
        _tabBg = null;
        _tabActiveBg = null;
    }
}
