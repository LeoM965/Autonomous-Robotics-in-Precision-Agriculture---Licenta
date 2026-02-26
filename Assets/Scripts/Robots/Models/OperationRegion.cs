using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;

namespace Robots.Models
{
    public class OperationRegion
    {
        public Rect Bounds { get; private set; }

        public OperationRegion(Rect bounds)
        {
            Bounds = bounds;
        }

        public bool IsWithinRegion(Vector3 position)
        {
            return Bounds.Contains(new Vector2(position.x, position.z), true);
        }

        public List<EnvironmentalSensor> FilterParcels(IEnumerable<EnvironmentalSensor> allParcels)
        {
            var filtered = new List<EnvironmentalSensor>();
            foreach (var parcel in allParcels)
            {
                if (parcel != null && IsWithinRegion(parcel.transform.position))
                {
                    filtered.Add(parcel);
                }
            }
            return filtered;
        }

        public static OperationRegion FromZone(FenceZone zone)
        {
            float minX = Mathf.Min(zone.startXZ.x, zone.endXZ.x);
            float minZ = Mathf.Min(zone.startXZ.y, zone.endXZ.y);
            float width = Mathf.Abs(bestWidth(zone));
            float height = Mathf.Abs(bestHeight(zone));
            return new OperationRegion(new Rect(minX, minZ, width, height));
        }

        private static float bestWidth(FenceZone zone) => zone.endXZ.x - zone.startXZ.x;
        private static float bestHeight(FenceZone zone) => zone.endXZ.y - zone.startXZ.y;
    }
}
