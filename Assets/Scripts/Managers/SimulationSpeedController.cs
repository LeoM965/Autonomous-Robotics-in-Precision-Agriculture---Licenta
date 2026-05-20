using UnityEngine;
using System.Collections;
using Weather.Components;

public class SimulationSpeedController : MonoBehaviour
{
    public static SimulationSpeedController Instance;

    [SerializeField] private float[] speeds = { 0f, 1f, 2f, 5f, 10f };
    [SerializeField] private float boostMultiplier = 5f;
    [SerializeField] private float idleGracePeriod = 3f;
    [SerializeField] private float minOperatingTemp = 0f;

    private int currentIndex = 1;
    private bool isBoostActive;
    private bool isSkipping;
    private bool isPausedInternally;
    private bool autoSkipEnabled;
    private float skipTimer;

    public float[] Speeds => speeds;
    public int CurrentIndex => currentIndex;
    public bool IsBoostActive => isBoostActive;
    public bool IsSkipping => isSkipping;
    public bool AutoSkipEnabled => autoSkipEnabled;
    public float BoostMultiplier => boostMultiplier;
    public bool ShouldRobotsStop => autoSkipEnabled && TooCold();

    private bool TooCold() =>
        WeatherSystem.Instance != null && WeatherSystem.Instance.CurrentTemperature < minOperatingTemp;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start() => SetSpeed(currentIndex);

    private void Update()
    {
        if (!autoSkipEnabled || isSkipping || isPausedInternally) return;
        if (speeds[currentIndex] <= 0f) return;

        bool cold = TooCold();

        if (AllRobotsIdle())
        {
            skipTimer += Time.unscaledDeltaTime;
            float wait = cold ? 0.5f : idleGracePeriod;
            if (skipTimer >= wait)
            {
                skipTimer = 0f;
                SkipDay();
            }
        }
        else
        {
            skipTimer = 0f;
        }
    }

    private bool AllRobotsIdle()
    {
        var operators = FindObjectsByType<RobotOperator>(FindObjectsSortMode.None);
        var flights = FindObjectsByType<Robots.Capabilities.Flight.AgroBotFlight>(FindObjectsSortMode.None);

        if (operators.Length == 0 && flights.Length == 0) return false;

        foreach (var op in operators)
        {
            if (op.CurrentState != RobotOperator.OperatorState.Idle)
                return false;
        }

        foreach (var flight in flights)
        {
            if (flight.GetStatus() != "Idle" && flight.GetStatus() != "Idle - Nicio parcelă nu necesită tratament")
                return false;
        }

        return true;
    }

    public void ToggleAutoSkip()
    {
        autoSkipEnabled = !autoSkipEnabled;
        skipTimer = 0f;
    }

    public void SetSpeed(int index)
    {
        if (isSkipping) return;
        currentIndex = Mathf.Clamp(index, 0, speeds.Length - 1);
        ApplyTimeScale();
    }

    public void ToggleBoost()
    {
        if (isSkipping) return;
        isBoostActive = !isBoostActive;
        ApplyTimeScale();
    }

    public void SetPaused(bool paused)
    {
        isPausedInternally = paused;
        ApplyTimeScale();
    }

    public void SkipDay() 
    {
        if (TimeManager.Instance == null) return;
        
        float currentTotal = TimeManager.Instance.totalSimulatedHours;
        float currentDayStart = Mathf.Floor(currentTotal / 24f) * 24f;
        float targetTotal = currentDayStart + 8f;
        
        // Dacă suntem deja la 8:00 (sau foarte aproape, cum ar fi 7:59)
        if (targetTotal <= currentTotal + 0.05f) 
        {
            targetTotal += 24f;
        }
        
        float hoursToNextMorning = targetTotal - currentTotal;
        SkipTimeGradual(hoursToNextMorning, 2.5f);
    }

    public void SkipTimeGradual(float hours, float realTimeDuration)
    {
        if (!isSkipping) StartCoroutine(PerformSkip(hours, realTimeDuration));
    }

    private void ApplyTimeScale()
    {
        if (isPausedInternally)
        {
            Time.timeScale = 0f;
            return;
        }

        float scale = speeds[currentIndex];
        if (isBoostActive && scale > 0f)
            scale *= boostMultiplier;

        Time.timeScale = scale;
        Time.fixedDeltaTime = Mathf.Clamp(0.02f * scale, 0.02f, 0.08f);
    }

    private IEnumerator PerformSkip(float totalHours, float realDuration)
    {
        if (TimeManager.Instance == null) yield break;

        isSkipping = true;
        Time.timeScale = 0f;

        float elapsed = 0f;
        float hoursAdvanced = 0f;

        while (elapsed < realDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float target = Mathf.Clamp01(elapsed / realDuration) * totalHours;
            float chunk = target - hoursAdvanced;

            if (chunk > 0f)
            {
                TimeManager.Instance.AdvanceTime(chunk);
                hoursAdvanced = target;
            }
            yield return null;
        }

        if (hoursAdvanced < totalHours)
            TimeManager.Instance.AdvanceTime(totalHours - hoursAdvanced);

        isSkipping = false;
        ApplyTimeScale();
    }
}
