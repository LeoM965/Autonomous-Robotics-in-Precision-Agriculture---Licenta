using UnityEngine;
using Sensors.Components;

public class HarvestExecutor
{
    private EnvironmentalSensor parcel;
    private int harvestedInParcel;
    private float harvestTimer;
    private float harvestDelay;

    public int HarvestedInParcel => harvestedInParcel;

    public void SetTarget(EnvironmentalSensor targetParcel, float delay)
    {
        parcel = targetParcel;
        harvestDelay = delay;
        harvestedInParcel = 0;
    }

    public bool UpdateHarvest(CropGrowth crop)
    {
        if (crop == null || crop.IsBeingHarvested || !crop.IsFullyGrown) return true;

        harvestTimer += Time.deltaTime;
        if (harvestTimer >= harvestDelay)
        {
            crop.Harvest();
            harvestedInParcel++;
            harvestTimer = 0f;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        parcel = null;
        harvestedInParcel = 0;
    }
}
