using UnityEngine;
[System.Serializable]
public class MapConfig
{
    [Range(20, 100)]
    public float sizePercent = 30f;
    [Range(0, 100)]
    public float offsetX = 20f;
    [Range(0, 100)]
    public float offsetY = 20f;
    [Range(0, 10)]
    public int borderWidth = 2;
    [Range(0, 20)]
    public int gridLines = 5;
    [Range(1, 20)]
    public float robotSize = 8f;
    public Rect GetMapRect()
    {
        float size = Screen.height * sizePercent * 0.01f;
        float x = Screen.width - size - offsetX;
        float y = Screen.height - size - offsetY;
        return new Rect(x, y, size, size);
    }
    public Rect GetHeaderRect(Rect mapRect)
    {
        return new Rect(mapRect.x, mapRect.y - 20, mapRect.width, 20);
    }
}
