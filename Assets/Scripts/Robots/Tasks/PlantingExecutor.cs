using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

public class PlantingExecutor
{
    private EnvironmentalSensor parcel;
    private CropData crop;
    private GameObject prefab;
    private int plantsPlaced;
    private float totalCost;
    
    public int PlantsPlaced { get { return plantsPlaced; } }
    public float TotalCost { get { return totalCost; } }
    
    public void SetTarget(EnvironmentalSensor targetParcel, CropData selectedCrop, GameObject cropPrefab)
    {
        parcel = targetParcel;
        crop = selectedCrop;
        prefab = cropPrefab;
    }
    
    public void PlantAt(Vector3 position)
    {
        if (prefab == null || parcel == null)
        {
            Debug.LogWarning("[PlantingExecutor] Skipping plant point: Prefab or Parcel is null.");
            return;
        }
            
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        GameObject plantObject;
        
        if (CropPool.Instance != null)
            plantObject = CropPool.Instance.Get(prefab, position, rotation, parcel.transform);
        else
            plantObject = Object.Instantiate(prefab, position, rotation, parcel.transform);
            
        if (plantsPlaced == 0 && parcel != null && crop != null)
        {
            parcel.plantedVarietyId = crop.id;
            parcel.plantedVarietyName = crop.name;
        }

        if (crop != null)
        {
            totalCost += crop.seedCostEUR;
            var growth = plantObject.GetComponent<CropGrowth>();
            if (growth != null)
            {
                growth.Init(crop.growthTimeSeconds);
                parcel.activeCrops.Add(growth);
                plantsPlaced++;
            }
        }
    }
    
    
    public void Reset()
    {
        parcel = null;
        crop = null;
        prefab = null;
        plantsPlaced = 0;
        totalCost = 0;
    }
}
