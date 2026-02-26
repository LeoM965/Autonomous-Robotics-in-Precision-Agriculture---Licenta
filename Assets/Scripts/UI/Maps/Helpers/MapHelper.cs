using UnityEngine;
public static class MapHelper
{
    private static Texture2D _tex;
    public static Texture2D Tex
    {
        get
        {
            if (_tex == null)
                _tex = Texture2D.whiteTexture;
            return _tex;
        }
    }
    public static void DrawBox(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Tex);
        GUI.color = Color.white;
    }
    public static void DrawShadow(Rect r, float offset)
    {
        GUI.color = new Color(0, 0, 0, 0.4f);
        GUI.DrawTexture(new Rect(r.x + offset, r.y + offset, r.width, r.height), Tex);
        GUI.color = Color.white;
    }
    public static void DrawBorder(Rect r, Color c, int t)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Tex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Tex);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Tex);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Tex);
        GUI.color = Color.white;
    }

    public static Vector2 WorldToMap(Vector3 worldPos, Vector3 terrainPos, float invX, float invZ, Rect map)
    {
        float x = map.x + (worldPos.x - terrainPos.x) * invX * map.width;
        float y = map.y + (worldPos.z - terrainPos.z) * invZ * map.height;
        return new Vector2(x, y);
    }
    public static void DrawDot(Vector2 pos, float size, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), Tex);
        GUI.color = Color.white;
    }
    public static void DrawPulse(Vector2 pos, float size, Color c, float pulse)
    {
        float a = (Mathf.Sin(pulse) + 1f) * 0.5f * 0.3f;
        GUI.color = new Color(c.r, c.g, c.b, a);
        GUI.DrawTexture(new Rect(pos.x - size, pos.y - size, size * 2, size * 2), Tex);
        GUI.color = Color.white;
    }
    public static bool ClickedIn(Rect area, Event e)
    {
        return e.type == EventType.MouseDown && area.Contains(e.mousePosition);
    }
}
