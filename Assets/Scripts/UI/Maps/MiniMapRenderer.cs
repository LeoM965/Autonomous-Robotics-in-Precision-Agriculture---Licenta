using UnityEngine;
public static class MiniMapRenderer
{
    public static void DrawZone(Rect map, Vector2 start, Vector2 end, float invX, float invZ, Color c)
    {
        Vector2 min = new Vector2(Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y));
        Vector2 max = new Vector2(Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));
        float w = (max.x - min.x) * invX * map.width;
        float h = (max.y - min.y) * invZ * map.height;
        float x = map.x + start.x * invX * map.width;
        float y = map.y + start.y * invZ * map.height;
        float x2 = map.x + end.x * invX * map.width;
        float y2 = map.y + end.y * invZ * map.height;
        Rect r = new Rect(Mathf.Min(x, x2), Mathf.Min(y, y2), Mathf.Abs(x - x2), Mathf.Abs(y - y2));
        MapHelper.DrawBox(r, c);
    }
    public static void DrawBuilding(Rect map, Building b, Vector3 terrainPos, float invX, float invZ, Color c, Event e)
    {
        Vector2 pos = MapHelper.WorldToMap(b.position, terrainPos, invX, invZ, map);
        MapHelper.DrawDot(pos, 6, c);
        if (MapHelper.ClickedIn(new Rect(pos.x - 5, pos.y - 5, 10, 10), e))
        {
        }
    }
    public static bool DrawRobot(Rect map, Vector3 pos, Vector3 terrainPos, float invX, float invZ, Color c, float size, bool selected, float pulse, Event e)
    {
        Vector2 mapPos = MapHelper.WorldToMap(pos, terrainPos, invX, invZ, map);
        if (selected)
            MapHelper.DrawPulse(mapPos, size, c, pulse);
        MapHelper.DrawDot(mapPos, size, c);
        return MapHelper.ClickedIn(new Rect(mapPos.x - size, mapPos.y - size, size * 2, size * 2), e);
    }
}
