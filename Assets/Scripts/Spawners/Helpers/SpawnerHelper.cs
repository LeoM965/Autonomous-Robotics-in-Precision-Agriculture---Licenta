using UnityEngine;
public static class SpawnerHelper
{
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
