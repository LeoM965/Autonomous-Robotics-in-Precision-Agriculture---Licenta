using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class ParcelCache : MonoBehaviour
{
    private static ParcelCache instance;
    private static bool isQuitting;
    private Dictionary<int, EnvironmentalSensor> cache = new Dictionary<int, EnvironmentalSensor>();
    private List<EnvironmentalSensor> _cachedList = new List<EnvironmentalSensor>();
    private bool _cacheDirty = true;
    
    public static bool HasInstance => instance != null && !isQuitting;
    
    public static ParcelCache Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                instance = FindFirstObjectByType<ParcelCache>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ParcelCache");
                    instance = go.AddComponent<ParcelCache>();
                }
            }
            return instance;
        }
    }

    private void OnApplicationQuit() => isQuitting = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void Register(EnvironmentalSensor parcel)
    {
        if (parcel == null) return;
        int id = parcel.GetInstanceID();
        if (!cache.ContainsKey(id))
        {
            cache.Add(id, parcel);
            _cacheDirty = true;
        }
    }

    public void Unregister(EnvironmentalSensor parcel)
    {
        if (parcel == null) return;
        if (cache.Remove(parcel.GetInstanceID()))
        {
            _cacheDirty = true;
        }
    }

    public EnvironmentalSensor Get(int id)
    {
        if (cache.TryGetValue(id, out EnvironmentalSensor parcel))
            return parcel;
        return null;
    }

    public List<EnvironmentalSensor> GetAll()
    {
        if (_cacheDirty)
        {
            _cachedList.Clear();
            _cachedList.AddRange(cache.Values);
            _cacheDirty = false;
        }
        return _cachedList;
    }

    public IEnumerable<EnvironmentalSensor> ParcelsIterator => cache.Values;

    public static List<EnvironmentalSensor> Parcels
    {
        get
        {
            if (Instance == null) return new List<EnvironmentalSensor>();
            return Instance.GetAll();
        }
    }

    public void Clear() => cache.Clear();
}
