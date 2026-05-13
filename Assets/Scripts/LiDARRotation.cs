using UnityEngine;

/// <summary>
/// Visual indicator component: rotation speed reflects the parent robot's operational state.
/// Spins fast while the robot is actively working, slow while idle.
/// Attached directly to the LiDAR mesh on robot prefabs in the Unity Editor.
/// </summary>
public class LiDARRotation : MonoBehaviour
{
    [SerializeField] private float activeSpeed = 180f;
    [SerializeField] private float idleSpeed = 30f;

    private RobotOperator[] operators;

    private void Start()
    {
        operators = GetComponentsInParent<RobotOperator>();
    }

    void Update()
    {
        float speed = idleSpeed;

        if (operators != null)
        {
            foreach (var op in operators)
            {
                if (op != null && op.CurrentState == RobotOperator.OperatorState.Working)
                {
                    speed = activeSpeed;
                    break;
                }
            }
        }

        transform.Rotate(0, speed * Time.deltaTime, 0, Space.Self);
    }
}