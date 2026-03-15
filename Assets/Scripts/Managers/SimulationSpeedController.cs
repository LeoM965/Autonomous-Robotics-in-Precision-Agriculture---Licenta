using UnityEngine;
using System.Collections;

public class SimulationSpeedController : MonoBehaviour
{
    public static SimulationSpeedController Instance;

    [SerializeField] private float[] speeds = { 0f, 1f, 2f, 5f, 10f };
    [SerializeField] private float boostMultiplier = 5f;

    private int currentIndex = 1;
    private bool isBoostActive;
    private bool isSkipping;
    private bool isPausedInternally;

    public float[] Speeds => speeds;
    public int CurrentIndex => currentIndex;
    public bool IsBoostActive => isBoostActive;
    public bool IsSkipping => isSkipping;
    public float BoostMultiplier => boostMultiplier;
    public float FairnessMultiplier => isSkipping ? 10f : 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (GetComponent<SimulationSpeedUI>() == null)
            gameObject.AddComponent<SimulationSpeedUI>();
        SetSpeed(currentIndex);
    }

    public void SetSpeed(int index)
    {
        if (isSkipping) return;
        currentIndex = Mathf.Clamp(index, 0, speeds.Length - 1);
        UpdateSimulationTime();
    }

    public void ToggleBoost()
    {
        if (isSkipping) return;
        isBoostActive = !isBoostActive;
        UpdateSimulationTime();
    }

    public void SetPaused(bool paused)
    {
        isPausedInternally = paused;
        UpdateSimulationTime();
    }

    public void SkipDay()
    {
        if (!isSkipping) StartCoroutine(SkipDayGradual());
    }

    public void UpdateSimulationTime()
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

    private IEnumerator SkipDayGradual()
    {
        if (TimeManager.Instance == null) yield break;

        isSkipping = true;
        float realDuration = 3f;
        float elapsed = 0f;
        float totalHours = 24f;
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
    }
}
