using UnityEngine;

namespace Robots.Models
{
    [System.Serializable]
    public class RobotBattery
    {
        public float maxKWh = 1f;
        public float currentKWh = 1f;
        public float consumptionMeter = 0f;
        public float consumptionWorkSec = 0f;
        public float consumptionStandbySec = 0f;
        public float rechargeRate = 5f;

        public float Percentage => currentKWh / maxKWh;
        public bool IsCritical => currentKWh < maxKWh * 0.15f;
        public bool IsFull => currentKWh >= maxKWh * 0.99f;

        public void Consume(float amount)
        {
            currentKWh = Mathf.Max(0, currentKWh - amount);
        }

        public void Recharge(float deltaTime)
        {
            currentKWh = Mathf.Min(maxKWh, currentKWh + rechargeRate * deltaTime);
        }

        public bool CanPerformTask(float distance, float workTime)
        {
            float totalCost = (distance * consumptionMeter) + (workTime * consumptionWorkSec);
            float safetyBuffer = maxKWh * 0.2f;
            return currentKWh > (totalCost + safetyBuffer);
        }
    }
}
