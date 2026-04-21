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
    private bool isIdle;
    private float accumulatedEnergy;
    private float accumulatedDist;
    private float accumulatedSimHours;
    private int reportOffset;

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
        reportOffset = Mathf.Abs(GetInstanceID()) % 10;
        
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
        if (isCharging)
        {
            battery.Recharge(Time.deltaTime);

            if (battery.IsFull) 
            {
                isCharging = false;
                OnBatteryChanged?.Invoke(BatteryPercent);
            }
            else if (Time.frameCount % 10 == 0)
            {
                OnBatteryChanged?.Invoke(BatteryPercent);
            }
            return;
        }

        // Cand robotul e in Idle, nu consuma energie si nu inregistreaza distanta/costuri
        if (isIdle)
        {
            lastPosition = transform.position;
            return;
        }

        float consumed = 0f;
        float distSqr = (transform.position - lastPosition).sqrMagnitude;
        float reportDist = 0f;

        if (distSqr > 0.0001f) // Aprox 0.01m threshold (0.01^2 = 0.0001)
        {
            float dist = Mathf.Sqrt(distSqr);
            consumed += dist * battery.consumptionMeter;
            lastPosition = transform.position;
            reportDist = dist;
            
            if (TimeManager.Instance != null)
                TimeManager.Instance.AddDistanceTraveled(dist);
        }

        float dt = Time.deltaTime;
        consumed += (isWorking ? battery.consumptionWorkSec : battery.consumptionStandbySec) * dt;

        if (consumed > 0)
        {
            Consume(consumed);

            accumulatedEnergy += consumed;
            accumulatedDist += reportDist;
            accumulatedSimHours += dt / 3600f;
            
            if ((Time.frameCount + reportOffset) % 10 == 0 && RobotEconomicsManager.Instance != null)
            {
                RobotEconomicsManager.Instance.RecordStatus(transform, accumulatedEnergy, accumulatedDist, accumulatedSimHours);
                accumulatedEnergy = 0f;
                accumulatedDist = 0f;
                accumulatedSimHours = 0f;
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
    public void SetIdle(bool idle) 
    { 
        isIdle = idle;
        if (RobotEconomicsManager.Instance != null)
            RobotEconomicsManager.Instance.SetRobotIdle(transform, idle);
    }

    public bool HasEnoughEnergy(float estimatedDistance, float estimatedWorkSeconds = 0f)
    {
        return battery.CanPerformTask(estimatedDistance, estimatedWorkSeconds);
    }
}
