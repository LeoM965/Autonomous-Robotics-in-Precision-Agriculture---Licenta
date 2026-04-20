using UnityEngine;
using System;
using System.Globalization;
using Weather.Models;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Time & Distance Settings")]
    [Tooltip("Real-world speed of the robot in km/h used for calculation.")]
    [SerializeField] private float robotRealSpeedKmh = 5f; 
    
    [Header("Current State")]
    public float totalSimulatedHours = 8f; 
    public float TotalSimulatedHours => totalSimulatedHours;
    public bool IsInitialized { get; private set; } = true;

    public int currentDay => Mathf.FloorToInt(totalSimulatedHours / 24) + 1;
    public float timeOfDay => totalSimulatedHours % 24;

    private readonly DateTime startDate = new DateTime(2024, 1, 1);
    public DateTime CurrentDate => startDate.AddHours(totalSimulatedHours);
    
    public event Action OnDayChanged; 
    public event Action<float> OnHourChanged;
    public event Action<Season> OnSeasonChanged;

    private float secondsPerMeter;
    private int activeRobotCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        float speedMps = (robotRealSpeedKmh * 1000f) / 3600f;
        secondsPerMeter = 1f / speedMps;
    }

    public void RegisterRobot() => activeRobotCount++;
    public void UnregisterRobot() => activeRobotCount = Mathf.Max(0, activeRobotCount - 1);

    public void AddDistanceTraveled(float distanceMeters)
    {
        float hours = (distanceMeters * secondsPerMeter) / Mathf.Max(1, activeRobotCount) / 3600f;
        AdvanceTime(hours);
    }
    
    public void AddWorkTime(float simulatedSeconds)
    {
        float hours = (simulatedSeconds / Mathf.Max(1, activeRobotCount)) / 3600f;
        AdvanceTime(hours);
    }

    public void AdvanceTime(float hoursToAdd)
    {
        Season oldSeason = GetCurrentSeason();
        
        int oldHourCounter = Mathf.FloorToInt(totalSimulatedHours);
        int oldDayCounter = Mathf.FloorToInt(totalSimulatedHours / 24f);

        totalSimulatedHours += hoursToAdd;

        int newHourCounter = Mathf.FloorToInt(totalSimulatedHours);
        int newDayCounter = Mathf.FloorToInt(totalSimulatedHours / 24f);

        int hoursPassed = newHourCounter - oldHourCounter;
        int daysPassed = newDayCounter - oldDayCounter;

        // Trigger hourly events reliably for every discrete hour passed
        for (int i = 0; i < hoursPassed; i++)
        {
            float simulatedHourOfDay = (oldHourCounter + i + 1) % 24;
            OnHourChanged?.Invoke(simulatedHourOfDay);
        }

        // Trigger daily events reliably for every discrete day passed (Crucial for Economy & Growth Simulation)
        for (int i = 0; i < daysPassed; i++)
        {
            OnDayChanged?.Invoke();
        }

        Season newSeason = GetCurrentSeason();
        if (newSeason != oldSeason)
        {
            OnSeasonChanged?.Invoke(newSeason);
        }
    }

    public void SkipToDate(int day, int month)
    {
        try
        {
            DateTime current = CurrentDate;
            DateTime target = new DateTime(current.Year, month, day, current.Hour, current.Minute, current.Second, current.Millisecond);

            if (target <= current) target = target.AddYears(1);

            double hours = (target - current).TotalHours;
            AdvanceTime((float)hours);
            Debug.Log($"[TimeManager] Jumped to {target:d MMM yyyy}");
        }
        catch (Exception)
        {
            Debug.LogError($"[TimeManager] Data invalida: Zi {day}, Luna {month}");
        }
    }

    public Season GetCurrentSeason()
    {
        int month = CurrentDate.Month;
        if (month >= 3 && month <= 5) return Season.Spring;
        if (month >= 6 && month <= 8) return Season.Summer;
        if (month >= 9 && month <= 11) return Season.Autumn;
        return Season.Winter;
    }
}
