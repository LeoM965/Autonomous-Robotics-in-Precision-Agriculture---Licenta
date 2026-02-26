using UnityEngine;
using System.Collections.Generic;

public class CropManager : MonoBehaviour
{
    public static CropManager Instance { get; private set; }

    private readonly List<CropGrowth> _activeCrops = new List<CropGrowth>(4096);
    private readonly Dictionary<CropGrowth, int> _cropIndices = new Dictionary<CropGrowth, int>();
    
    [Header("Optimization")]
    [SerializeField] private int updatesPerFrame = 512;
    private int _lastUpdateIndex;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CropManager");
            go.AddComponent<CropManager>();
        }
    }

    public void RegisterCrop(CropGrowth crop)
    {
        if (crop == null || _cropIndices.ContainsKey(crop)) return;
        
        _cropIndices[crop] = _activeCrops.Count;
        _activeCrops.Add(crop);
    }

    public void UnregisterCrop(CropGrowth crop)
    {
        if (crop == null || !_cropIndices.TryGetValue(crop, out int index)) return;

        int lastIndex = _activeCrops.Count - 1;
        if (index < lastIndex)
        {
            CropGrowth lastCrop = _activeCrops[lastIndex];
            _activeCrops[index] = lastCrop;
            _cropIndices[lastCrop] = index;
        }

        _activeCrops.RemoveAt(lastIndex);
        _cropIndices.Remove(crop);
        
        if (_lastUpdateIndex >= _activeCrops.Count) 
            _lastUpdateIndex = 0;
    }

    private void Update()
    {
        int count = _activeCrops.Count;
        if (count == 0) return;

        float deltaTime = Time.deltaTime;
        int slice = Mathf.Min(count, updatesPerFrame);
        
        for (int i = 0; i < slice; i++)
        {
            _lastUpdateIndex = (_lastUpdateIndex + 1) % count;
            var crop = _activeCrops[_lastUpdateIndex];
            
            if (crop != null)
            {
                crop.ManualUpdate(deltaTime);
            }
        }
    }
}
