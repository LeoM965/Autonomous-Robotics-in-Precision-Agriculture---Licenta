using UnityEngine;

public static class CropLoader
{
    public static CropDatabase Load()
    {
        TextAsset json = Resources.Load<TextAsset>("CropData");
        if (json == null) return null;
        
        return JsonUtility.FromJson<CropDatabase>(json.text);
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
