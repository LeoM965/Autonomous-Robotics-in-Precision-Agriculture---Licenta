using UnityEngine;
using System;
using Weather.Models;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Time & Distance Settings")]
    [Tooltip("Real-world speed of the robot in km/h used for calculation.")]
    [SerializeField] private float robotRealSpeedKmh = 5f; 
    
    [Header("Current State")]
    public int currentDay = 1;
    public float timeOfDay = 8f; 
    
    public event Action<int> OnDayChanged;
    public event Action<float> OnHourChanged;
    public event Action<Season> OnSeasonChanged;

    private int totalDaysPassed = 0;
    private float secondsPerMeter;
    private int activeRobotCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        float speedMps = (robotRealSpeedKmh * 1000f) / 3600f;
        secondsPerMeter = 1f / speedMps;
        
        Debug.Log($"[TimeManager] Calibrated: 1 meter = {secondsPerMeter:F2} simulated seconds.");
    }

    public void RegisterRobot()
    {
        activeRobotCount++;
    }

    public void UnregisterRobot()
    {
        if (activeRobotCount > 0)
            activeRobotCount--;
    }

    public void AddDistanceTraveled(float distanceMeters)
    {
        int count = Mathf.Max(1, activeRobotCount);
        float simulatedSeconds = (distanceMeters * secondsPerMeter) / count;
        
        float hoursPassed = simulatedSeconds / 3600f;
        
        AdvanceTime(hoursPassed);
    }
    
    public void AddWorkTime(float simulatedSeconds)
    {
        int count = Mathf.Max(1, activeRobotCount);
        float hoursPassed = (simulatedSeconds / count) / 3600f;
        AdvanceTime(hoursPassed);
    }

    private void AdvanceTime(float hoursToAdd)
    {
        int oldHour = Mathf.FloorToInt(timeOfDay);
        
        timeOfDay += hoursToAdd;

        bool dayChanged = false;
        
        while (timeOfDay >= 24f)
        {
            timeOfDay -= 24f;
            currentDay++;
            totalDaysPassed++;
            dayChanged = true;
        }

        int newHour = Mathf.FloorToInt(timeOfDay);
        if (newHour != oldHour)
        {
            OnHourChanged?.Invoke(timeOfDay);
        }

        if (dayChanged)
        {
            OnDayChanged?.Invoke(currentDay);
            OnSeasonChanged?.Invoke(GetCurrentSeason());
        }
    }

    public Season GetCurrentSeason()
    {
        int dayOfYear = totalDaysPassed % 365;
        if (dayOfYear < 90) return Season.Spring;
        if (dayOfYear < 180) return Season.Summer;
        if (dayOfYear < 270) return Season.Autumn;
        return Season.Winter;
    }

    public string GetFormattedTime()
    {
        int h = Mathf.FloorToInt(timeOfDay);
        int m = Mathf.FloorToInt((timeOfDay - h) * 60);
        return $"Day {currentDay} - {h:00}:{m:00}";
    }
}
