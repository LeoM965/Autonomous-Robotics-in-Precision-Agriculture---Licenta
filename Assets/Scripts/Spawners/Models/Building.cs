using UnityEngine;
public enum BuildingType { Generic, Storage, ChargingStation, WaterTank }
[System.Serializable]
public class Building
{
    public BuildingType type;
    public Vector3 position;
    public Building(BuildingType type, Vector3 position)
    {
        this.type = type;
        this.position = position;
    }
}
