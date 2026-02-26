using UnityEngine;
using UnityEngine.Events;
using Robots.Models;

public class RobotEnergy : MonoBehaviour
{
    [SerializeField] private RobotBattery battery = new RobotBattery();

    public UnityEvent<float> OnBatteryChanged;
    public UnityEvent OnBatteryCritical;

    private Vector3 lastPosition;
    private bool isWorking;
    private bool isCharging;

    public float BatteryPercent => battery.Percentage;
    public float CurrentBattery => battery.currentKWh;

    private void Start()
    {
        lastPosition = transform.position;
        OnBatteryChanged?.Invoke(BatteryPercent);
    }

    private void Update()
    {
        if (isCharging)
        {
            battery.Recharge(Time.deltaTime);
            if (battery.IsFull) isCharging = false;
            OnBatteryChanged?.Invoke(BatteryPercent);
            return;
        }

        float consumed = 0f;
        float dist = Vector3.Distance(transform.position, lastPosition);
        
        if (dist > 0.001f)
        {
            consumed += dist * battery.consumptionMeter;
            lastPosition = transform.position;
        }

        if (isWorking) consumed += battery.consumptionWorkSec * Time.deltaTime;
        if (consumed > 0) Consume(consumed);
    }

    public void Consume(float amount)
    {
        battery.Consume(amount);
        OnBatteryChanged?.Invoke(BatteryPercent);
        if (battery.IsCritical) OnBatteryCritical?.Invoke();
    }

    public void StartCharging() => isCharging = true;
    public void SetWorking(bool working) => isWorking = working;

    public bool HasEnoughEnergy(float estimatedDistance, float estimatedWorkSeconds = 0f)
    {
        return battery.CanPerformTask(estimatedDistance, estimatedWorkSeconds);
    }

    public void SetFullBattery()
    {
        battery.currentKWh = battery.maxKWh;
        OnBatteryChanged?.Invoke(BatteryPercent);
    }
}
