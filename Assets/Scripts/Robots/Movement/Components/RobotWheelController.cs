using UnityEngine;

namespace Robots.Components.Movement
{
    public class RobotWheelController : MonoBehaviour
    {
        [Header("Wheels Configuration")]
        [SerializeField] private Transform[] wheels;
        [SerializeField] private float wheelRadius = 0.3f;

        private Vector3[] wheelAngles;
        private float wheelRotation;
        private Vector3 lastUpdatePos;

        private void Start()
        {
            InitializeWheels();
            lastUpdatePos = transform.position;
        }

        public void InitializeWheels()
        {
            if (wheels == null || wheels.Length == 0) return;
            wheelAngles = new Vector3[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                    wheelAngles[i] = wheels[i].localEulerAngles;
            }
        }

        private void Update()
        {
            if (wheelAngles == null || wheels == null) return;
            
            Vector3 move = transform.position - lastUpdatePos;
            move.y = 0;
            lastUpdatePos = transform.position;

            if (move.sqrMagnitude > 0.001f)
            {
                wheelRotation += move.magnitude / (wheelRadius * Mathf.PI * 2f) * 360f;
                RobotHelper.UpdateWheelRotation(wheels, wheelAngles, wheelRotation);
            }
        }

        public void SetWheels(Transform[] newWheels, float radius)
        {
            wheels = newWheels;
            wheelRadius = radius;
            InitializeWheels();
        }
    }
}
