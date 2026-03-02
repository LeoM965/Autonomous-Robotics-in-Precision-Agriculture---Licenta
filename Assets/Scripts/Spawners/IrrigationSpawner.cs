using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using Sensors.Models;

public class IrrigationSpawner : MonoBehaviour
{
    [SerializeField] GameObject sprinklerPrefab;
    [SerializeField] GameObject waterEffectPrefab;
    [SerializeField] float sprinklerScale = 3f;
    [SerializeField] float threshold = 35f;
    [SerializeField] float waterPerSecond = 0.5f;
    [SerializeField] float updateInterval = 0.5f;
    
    GameObject root;
    List<SprinklerData> sprinklers = new List<SprinklerData>();
    float nextUpdateTime;
    
    struct SprinklerData
    {
        public GameObject effect;
        
        public EnvironmentalSensor parcel;
    }
    
    void Start()
    {
        StartCoroutine(Initialize());
    }
    
    System.Collections.IEnumerator Initialize()
    {
        if (sprinklerPrefab == null)
            yield break;
        
        if (root == null)
            root = SpawnHelper.CreateRoot(transform, "Irrigation");
        
        yield return new WaitForSeconds(0.5f);
        
        int retries = 5;
        while (ParcelCache.Parcels.Count == 0 && retries > 0)
        {
            yield return new WaitForSeconds(0.5f);
            retries--;
        }
        
        Spawn();
    }
    
    void Spawn()
    {
        if (sprinklerPrefab == null || root == null)
            return;
        
        var parcels = ParcelCache.Parcels;
        if (parcels.Count == 0)
            parcels = new List<EnvironmentalSensor>(FindObjectsOfType<EnvironmentalSensor>());
        
        foreach (var p in parcels)
        {
            if (p == null)
                continue;
            
            Vector3 pos = p.transform.position;
            pos.y = TerrainHelper.GetHeight(pos);
            
            var go = Instantiate(sprinklerPrefab, pos, Quaternion.identity, root.transform);
            go.transform.localScale = Vector3.one * sprinklerScale;
            
            GameObject fx = null;
            if (waterEffectPrefab != null)
            {
                fx = Instantiate(waterEffectPrefab, pos + Vector3.up, Quaternion.identity, go.transform);
                fx.SetActive(false);
            }
            
            sprinklers.Add(new SprinklerData { effect = fx, parcel = p });
        }
    }
    
    void Update()
    {
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + updateInterval;
        
        for (int i = 0; i < sprinklers.Count; i++)
        {
            var s = sprinklers[i];
            if (s.parcel == null || s.parcel.composition == null) continue;
            
            bool active = s.parcel.composition.moisture < threshold;
            if (s.effect != null && s.effect.activeSelf != active) s.effect.SetActive(active);
            if (active) s.parcel.composition.irrigationRate += waterPerSecond;
        }
    }
    
    [ContextMenu("Clear")]
    public void Clear()
    {
        StopAllCoroutines();
        sprinklers.Clear();
        
        if (root != null)
        {
            DestroyImmediate(root);
            root = null;
        }
        
        SpawnHelper.ClearRoot(transform, "Irrigation");
    }
    
    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        Clear();
        root = SpawnHelper.CreateRoot(transform, "Irrigation");
        Spawn();
    }
}
