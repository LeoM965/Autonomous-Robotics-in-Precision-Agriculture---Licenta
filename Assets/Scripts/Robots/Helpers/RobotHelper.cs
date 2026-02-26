using UnityEngine;
using System.Collections.Generic;
public static class RobotHelper
{
    public static float CalculateGroundOffset(Transform robot)
    {
        Renderer renderer = robot.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return robot.position.y - renderer.bounds.min.y;
        return 0.3f;
    }

    public static void UpdateWheelRotation(Transform[] wheels, Vector3[] originalAngles, float rotation)
    {
        if (wheels == null || originalAngles == null)
            return;
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
            {
                wheels[i].localRotation = Quaternion.Euler(
                    rotation,
                    originalAngles[i].y,
                    originalAngles[i].z
                );
            }
        }
    }
}
