using UnityEngine;

public partial class RobotMovement
{
    void InitWheels()
    {
        if (wheels == null || wheels.Length == 0) return;
        wheelAngles = new Vector3[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] != null)
                wheelAngles[i] = wheels[i].localEulerAngles;
        }
    }

    void Update()
    {
        if (wheelAngles == null) return;
        Vector3 move = transform.position - lastUpdatePos;
        move.y = 0;
        lastUpdatePos = transform.position;
        wheelRotation += move.magnitude / (wheelRadius * Mathf.PI * 2f) * 360f;
        RobotHelper.UpdateWheelRotation(wheels, wheelAngles, wheelRotation);
    }
}
