using UnityEngine;

public static class CropLoader
{
    private static CropDatabase cachedDB;

    public static CropDatabase Load()
    {
        if (cachedDB != null) return cachedDB;
        TextAsset json = Resources.Load<TextAsset>("CropData");
        if (json == null) return null;
        cachedDB = JsonUtility.FromJson<CropDatabase>(json.text);
        return cachedDB;
    }

    public static GameObject LoadPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string filename = System.IO.Path.GetFileNameWithoutExtension(path);
        GameObject prefab = Resources.Load<GameObject>("CropPrefabs/" + filename);
        if (prefab != null) return prefab;

        prefab = Resources.Load<GameObject>(path.Replace(".prefab", ""));
        if (prefab == null)
            Debug.LogWarning($"[CropLoader] Failed to load prefab. Tried 'CropPrefabs/{filename}' and '{path}'.");
        return prefab;
    }
}
