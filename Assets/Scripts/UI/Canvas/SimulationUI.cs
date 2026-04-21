using UnityEngine;
using TMPro;
using Weather.Components;

namespace UI.Canvas
{
    public class SimulationUI : MonoBehaviour
    {
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI dateText;
        public TextMeshProUGUI seasonText;
        public TextMeshProUGUI weatherText;
        public TextMeshProUGUI tempText;

        private float timer;

        void Update()
        {
            // Folosim unscaledDeltaTime pentru ca UI-ul sa mearga si in pauza/skip
            timer -= Time.unscaledDeltaTime;

            bool isSkipping = SimulationSpeedController.Instance != null && SimulationSpeedController.Instance.IsSkipping;
            
            // Daca dam skip, facem update mult mai des (la fiecare cadru) pentru fluiditate
            if (timer <= 0f || isSkipping)
            {
                Refresh();
                timer = 0.1f; // In mod normal, de 10 ori pe secunda e suficient
            }
        }

        private void Refresh()
        {
            if (TimeManager.Instance != null)
            {
                var date = TimeManager.Instance.CurrentDate;
                string timeStr = date.ToString("HH:mm");
                string dateStr = $"{date.Day} {GetMonthRomanian(date.Month)} {date.Year}";

                if (dateText)
                {
                    if (timeText) timeText.text = timeStr;
                    dateText.text = dateStr;
                }
                else if (timeText)
                {
                    timeText.text = $"{timeStr} | {dateStr}";
                }

                if (seasonText) seasonText.text = GetSeasonRomanian(TimeManager.Instance.GetCurrentSeason());
            }

            if (WeatherSystem.Instance != null)
            {
                if (weatherText) weatherText.text = GetWeatherRomanian(WeatherSystem.Instance.CurrentWeather);
                if (tempText) tempText.text = $"{WeatherSystem.Instance.CurrentTemperature:F1}°C";
            }
        }

        private string GetMonthRomanian(int m) => m switch
        {
            1 => "Ian", 2 => "Feb", 3 => "Mar", 4 => "Apr", 5 => "Mai", 6 => "Iun",
            7 => "Iul", 8 => "Aug", 9 => "Sep", 10 => "Oct", 11 => "Noi", 12 => "Dec",
            _ => "N/A"
        };

        private string GetSeasonRomanian(Weather.Models.Season s) => s switch
        {
            Weather.Models.Season.Spring => "Primăvară",
            Weather.Models.Season.Summer => "Vară",
            Weather.Models.Season.Autumn => "Toamnă",
            Weather.Models.Season.Winter => "Iarnă",
            _ => "N/A"
        };

        private string GetWeatherRomanian(Weather.Models.WeatherType t) => t switch
        {
            Weather.Models.WeatherType.Sunny => "Însorit",
            Weather.Models.WeatherType.Rainy => "Ploaie",
            Weather.Models.WeatherType.Stormy => "Furtună",
            Weather.Models.WeatherType.Foggy => "Ceață",
            Weather.Models.WeatherType.Snowy => "Zăpadă",
            _ => t.ToString()
        };
    }
}
