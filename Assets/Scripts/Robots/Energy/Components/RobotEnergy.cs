using UnityEngine;
using UnityEngine.Events;
using Robots.Models;
using Economics.Managers;
using Settings;

public class RobotEnergy : MonoBehaviour
{
    [SerializeField] private RobotBattery battery = new RobotBattery();

    public UnityEvent<float> OnBatteryChanged;
    public UnityEvent OnBatteryCritical;

    private Vector3 lastPosition;
    private bool isWorking;
    private bool isCharging;
    private float accumulatedEnergy;
    private float accumulatedDist;
    private float accumulatedSimHours;

    public float BatteryPercent => battery.Percentage;
    public float CurrentBattery => battery.currentKWh;
    public bool IsFull => battery.IsFull;
    public bool IsCharging => isCharging;
    public bool IsWorking => isWorking;

    private void OnEnable()
    {
        SimulationSettings.OnSettingsChanged += UpdateFromStaticData;
    }

    private void OnDisable()
    {
        SimulationSettings.OnSettingsChanged -= UpdateFromStaticData;
    }

    private void Start()
    {
        lastPosition = transform.position;
        
        if (GetComponent<BatteryBarUI>() == null)
            gameObject.AddComponent<BatteryBarUI>();

        UpdateFromStaticData();
        OnBatteryChanged?.Invoke(BatteryPercent);
    }

    public void UpdateFromStaticData()
    {
        var data = RobotDataLoader.FindByName(name);
        if (data != null)
        {
            battery.maxKWh = data.batteryCapacity / 1000f;
            battery.currentKWh = Mathf.Min(battery.currentKWh, battery.maxKWh);
            
            battery.consumptionMeter = data.consumptionMeter;
            battery.consumptionWorkSec = data.consumptionWorkSec;
            battery.consumptionStandbySec = data.consumptionStandbySec;
            if (data.rechargeRate > 0) battery.rechargeRate = data.rechargeRate;
        }
    }

    private void Update()
    {
        float multiplier = SimulationSpeedController.Instance != null ? SimulationSpeedController.Instance.FairnessMultiplier : 1f;

        if (isCharging)
        {
            battery.Recharge(Time.deltaTime * multiplier);
            
            if (TimeManager.Instance != null)
                TimeManager.Instance.AddWorkTime(Time.deltaTime);

            if (battery.IsFull) 
            {
                isCharging = false;
            }
            OnBatteryChanged?.Invoke(BatteryPercent);
            return;
        }

        float consumed = 0f;
        float dist = Vector3.Distance(transform.position, lastPosition);
        bool isActive = dist > 0.001f || isWorking;

        if (dist > 0.001f)
        {
            consumed += dist * battery.consumptionMeter;
            lastPosition = transform.position;
            
            if (TimeManager.Instance != null)
                TimeManager.Instance.AddDistanceTraveled(dist);
        }

        float effectiveDT = Time.deltaTime * multiplier;

        consumed += (isWorking ? battery.consumptionWorkSec : battery.consumptionStandbySec) * effectiveDT;

        if (consumed > 0)
        {
            Consume(consumed);
            
            if (isActive)
            {
                accumulatedEnergy += consumed;
                accumulatedDist += dist;
                accumulatedSimHours += effectiveDT / 3600f;
                
                if (Time.frameCount % 10 == 0 && RobotEconomicsManager.Instance != null)
                {
                    RobotEconomicsManager.Instance.RecordStatus(transform, accumulatedEnergy, accumulatedDist, accumulatedSimHours);
                    accumulatedEnergy = 0f;
                    accumulatedDist = 0f;
                    accumulatedSimHours = 0f;
                }
            }
        }
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
        isCharging = false;
        OnBatteryChanged?.Invoke(BatteryPercent);
    }
}
