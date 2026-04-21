using UnityEngine;
public static class SpawnHelper
{
    public static Vector3 RandomPositionInZone(Vector2 zoneMin, Vector2 zoneMax, float margin)
    {
        float x = Random.Range(zoneMin.x + margin, zoneMax.x - margin);
        float z = Random.Range(zoneMin.y + margin, zoneMax.y - margin);
        return new Vector3(x, 0, z);
    }
    public static Quaternion RandomYRotation()
    {
        float angle = Random.Range(0f, 360f);
        return Quaternion.Euler(0, angle, 0);
    }
    public static int PositionHash(Vector3 position)
    {
        const float precision = 2f;
        int x = Mathf.RoundToInt(position.x * precision);
        int y = Mathf.RoundToInt(position.y * precision);
        int z = Mathf.RoundToInt(position.z * precision);
        return x * 73856093 ^ y * 19349663 ^ z * 83492791;
    }

    public static GameObject CreateRoot(Transform parent, string name, bool isStatic = false)
    {
        var root = new GameObject(name) { isStatic = isStatic };
        root.transform.SetParent(parent);
        return root;
    }

    public static void ClearRoot(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
    }

    public static TextMesh CreateTextLabel(Transform parent, string text, Vector3 localPos)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.Euler(0, 180, 0);
        var tm = obj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = 0.15f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        return tm;
    }
}

