using System.Collections.Generic;
using System.Linq;
using Sensors.Components;

public static class ParcelHelper
{
    public static List<EnvironmentalSensor> GetParcelsInZone(FenceZone zone, float minQuality = 0f)
    {
        if (zone == null)
            return new List<EnvironmentalSensor>();
        return ParcelCache.Parcels
            .Where(p => p != null &&
                        p.composition != null &&
                        ZoneHelper.IsInZone(p.transform.position, zone) &&
                        p.LatestAnalysis.qualityScore >= minQuality)
            .OrderBy(p => ParseName(p.name))
            .ToList();
    }
    public static (char letter, int number) ParseName(string name)
    {
        name = name.Replace("Parcel_", "");
        char c = name.Length > 0 ? name[0] : 'Z';
        int.TryParse(name.Length > 1 ? name[1..] : "0", out int n);
        return (c, n);
    }
}
