using UnityEngine;
using System.Collections.Generic;

public class CropPool : MonoBehaviour
{
    public static CropPool Instance { get; private set; }
    
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<GameObject, string> activeObjects = new Dictionary<GameObject, string>();
    private Transform poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CropPool");
            go.AddComponent<CropPool>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        poolRoot = new GameObject("_CropPoolRoot").transform;
        poolRoot.SetParent(transform);
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        
        string key = prefab.name;
        GameObject obj;

        if (pools.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
        {
            obj = queue.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.transform.SetParent(parent);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, parent);
            obj.name = key;
        }

        activeObjects[obj] = key;
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;

        if (!activeObjects.TryGetValue(obj, out string key))
        {
            Destroy(obj);
            return;
        }

        activeObjects.Remove(obj);
        obj.SetActive(false);
        obj.transform.SetParent(poolRoot);

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();
        pools[key].Enqueue(obj);
    }
}
