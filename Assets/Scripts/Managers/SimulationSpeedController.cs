using UnityEngine;
using System.Collections;

public class SimulationSpeedController : MonoBehaviour
{
    [SerializeField] private float[] speeds = { 0f, 1f, 5f, 20f, 100f };
    [SerializeField] private float boostMultiplier = 5f;
    
    private int currentIndex = 1;
    private bool isBoostActive;
    private bool isSkipping;

    private void Start() => SetSpeed(currentIndex);

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

    private void UpdateSimulationTime()
    {
        float baseScale = speeds[currentIndex];
        float multiplier = 1f;

        if (isBoostActive)
        {
            if (baseScale > 0)
            {
                multiplier = boostMultiplier;
            }
        }

        Time.timeScale = baseScale * multiplier;
        
        if (Time.timeScale > 1f)
        {
            Time.fixedDeltaTime = 0.02f * Mathf.Lerp(1f, Time.timeScale, 0.5f);
        }
        else
        {
            float stepMultiplier = 0f;
            if (Time.timeScale > 0)
            {
                stepMultiplier = 1f;
            }
            Time.fixedDeltaTime = 0.02f * stepMultiplier;
        }
            
        Time.fixedDeltaTime = Mathf.Min(Time.fixedDeltaTime, 0.1f);
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

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 65, 250, 100));
        GUILayout.BeginVertical("box");
        
        GUILayout.BeginHorizontal();
        for (int i = 0; i < speeds.Length; i++)
        {
            if (currentIndex == i)
            {
                GUI.backgroundColor = Color.green;
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button(GetLabel(i), GUILayout.Width(35), GUILayout.Height(25)))
            {
                SetSpeed(i);
            }
        }
        
        if (isBoostActive)
        {
            GUI.backgroundColor = Color.cyan;
        }
        else
        {
            GUI.backgroundColor = Color.white;
        }

        if (GUILayout.Button("x" + boostMultiplier, GUILayout.Width(45), GUILayout.Height(25)))
        {
            ToggleBoost();
        }
        GUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
        
        string statusText = isSkipping ? "SKIPPING..." : (Time.timeScale > 0 ? "Speed: " + Time.timeScale + "x" : "PAUSED");

        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.richText = true;
        statusStyle.alignment = TextAnchor.MiddleCenter;
        
        GUILayout.Label("<b>" + statusText + "</b>", statusStyle);

        GUILayout.Space(5);
        GUI.enabled = !isSkipping;
        if (GUILayout.Button(isSkipping ? "Simulating..." : "Skip Day", GUILayout.Height(25)))
        {
            StartCoroutine(SkipDayGradual());
        }
        GUI.enabled = true;

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private string GetLabel(int i)
    {
        float val = speeds[i];
        if (val == 0)
        {
            return "||";
        }
        if (val == 1)
        {
            return ">";
        }
        return val.ToString();
    }

    private void Update()
    {
        if (isSkipping) return;
        
        for (int i = 0; i < speeds.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i - 1))
            {
                SetSpeed(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            SetSpeed(0);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBoost();
        }
    }
}
