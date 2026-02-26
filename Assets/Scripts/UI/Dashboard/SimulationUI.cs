using UnityEngine;
using TMPro;
using Weather.Components;

public class SimulationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI seasonText;
    [SerializeField] private TextMeshProUGUI weatherText;
    [SerializeField] private TextMeshProUGUI tempText;

    private void Update()
    {
        UpdateClock();
        UpdateWeather();
    }

    private void UpdateClock()
    {
        if (TimeManager.Instance != null && timeText != null)
        {
            timeText.text = TimeManager.Instance.GetFormattedTime();
            
            if (seasonText != null)
                seasonText.text = TimeManager.Instance.GetCurrentSeason().ToString();
        }
    }

    private void UpdateWeather()
    {
        if (WeatherSystem.Instance != null && weatherText != null)
        {
            weatherText.text = WeatherSystem.Instance.CurrentWeather.ToString();
            
            if (tempText != null)
                tempText.text = $"{WeatherSystem.Instance.CurrentTemperature:F1}°C";
        }
    }
}
