using UnityEngine;
using Sensors.Components;

public class HarvestExecutor
{
    private int harvestedInParcel;

    // Cached per-parcel (set once in SetTarget, used for all crops in parcel)
    private string varietyName;
    private int cropIndex;
    private float baseWeight;
    private float marketPrice;

    public int HarvestedInParcel => harvestedInParcel;

    public void SetTarget(EnvironmentalSensor targetParcel, CropDatabase db)
    {
        harvestedInParcel = 0;
        varietyName = targetParcel.plantedVarietyName;

        // Cache economic data once per parcel instead of per-crop
        cropIndex = db != null ? db.GetIndex(varietyName) : -1;
        var data = cropIndex >= 0 ? db.crops[cropIndex] : null;

        var yWeights = Settings.SimulationSettings.YieldWeights;
        var mPrices = Settings.SimulationSettings.MarketPrices;

        baseWeight = (yWeights != null && cropIndex >= 0 && cropIndex < yWeights.Length)
            ? yWeights[cropIndex] : (data?.yieldWeightKg ?? 1f);
        marketPrice = (mPrices != null && cropIndex >= 0 && cropIndex < mPrices.Length)
            ? mPrices[cropIndex] : (data?.marketPricePerKg ?? 1f);
    }

    public void UpdateHarvest(CropGrowth crop, Transform robotTransform)
    {
        if (crop == null || !crop.IsHarvestable) return;
        ReportRevenue(crop, robotTransform);
        crop.Harvest();
        harvestedInParcel++;
    }

    private void ReportRevenue(CropGrowth crop, Transform robot)
    {
        if (Economics.Managers.RobotEconomicsManager.Instance == null) return;

        var sensor = crop.ParentSensor;
        if (sensor == null) 
        {
            sensor = crop.GetComponentInParent<EnvironmentalSensor>();
            if (sensor == null) return;
        }

        float soilQuality = sensor.LatestAnalysis.qualityScore / 100f;
        float weight = baseWeight * soilQuality * crop.Progress;
        float revenue = marketPrice * weight;

        Economics.Managers.RobotEconomicsManager.Instance.AddRobotRevenue(robot, revenue);
        sensor.RecordHarvest(weight, revenue, crop.PurchasePrice);
    }

    public void Reset()
    {
        harvestedInParcel = 0;
        varietyName = null;
        cropIndex = -1;
    }
}
