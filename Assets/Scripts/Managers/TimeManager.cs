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
    
    [Header("Atmosphere Settings")]
    [SerializeField] private Material sunriseSkybox;
    [SerializeField] private Material sunsetSkybox;
    [SerializeField] private Material nightSkybox;

    [SerializeField] private float sunriseStart = 5f;
    [SerializeField] private float sunrisePeak = 6.5f;
    [SerializeField] private float sunriseEnd = 8f;
    
    [SerializeField] private float sunsetStart = 17f;
    [SerializeField] private float sunsetPeak = 18.5f;
    [SerializeField] private float sunsetEnd = 20f;

    private void UpdateEnvironment(float time)
    {
        Material targetSky = null;

        if (time >= sunriseStart && time < sunriseEnd)
        {
            targetSky = sunriseSkybox;
        }
        else if (time >= sunsetStart && time < sunsetEnd)
        {
            targetSky = sunsetSkybox;
        }
        else if (time >= sunsetEnd || time < sunriseStart)
        {
            targetSky = nightSkybox;
        }

        if (targetSky != null && RenderSettings.skybox != targetSky)
        {
            RenderSettings.skybox = targetSky;
            DynamicGI.UpdateEnvironment();
        }
    }
    
    public event Action<int> OnDayChanged;
    public event Action<float> OnHourChanged;
    public event Action<Season> OnSeasonChanged;
    public event Action<float> OnTimeJumped;

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

        UpdateEnvironment(timeOfDay);
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

    public void AdvanceTime(float hoursToAdd)
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

        UpdateEnvironment(timeOfDay);

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

    public void SkipDays(int days)
    {
        if (days <= 0) return;
        AdvanceTime(days * 24f);
        OnTimeJumped?.Invoke(days * 24f);
    }

    public void FireTimeJumped(float hours)
    {
        OnTimeJumped?.Invoke(hours);
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
