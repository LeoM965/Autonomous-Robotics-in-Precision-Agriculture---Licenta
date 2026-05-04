using System.Collections.Generic;
using System.IO;

namespace AI.Analytics.Export
{
    /// <summary>
    /// Acumuleaza date meteo pe ora si le exporta la sfarsitul zilei (append la fisier).
    /// </summary>
    public class WeatherExporter : ICsvExporter
    {
        private const string Header =
            "sim_run,day,hour,season,weather,temperature,crop_growth_mult,movement_penalty";

        private readonly List<WeatherHourEntry> log = new List<WeatherHourEntry>();

        /// <summary>
        /// Inregistreaza o intrare meteo (apelat la fiecare ora simulata).
        /// </summary>
        public void LogHour(int runId, float hour)
        {
            var tm = TimeManager.Instance;
            var ws = Weather.Components.WeatherSystem.Instance;

            log.Add(new WeatherHourEntry
            {
                runId  = runId,
                day    = tm != null ? tm.currentDay : 0,
                hour   = hour,
                season = tm != null ? tm.GetCurrentSeason().ToString() : "",
                weather       = ws != null ? ws.CurrentWeather.ToString() : "",
                temperature   = ws != null ? ws.CurrentTemperature : 0f,
                cropGrowthMult = ws != null ? ws.GetCropGrowthMultiplier() : 1f,
                movementPenalty = ws != null ? ws.GetMovementPenalty() : 1f
            });
        }

        public bool HasPendingData => log.Count > 0;

        public void Export(ExportContext ctx)
        {
            if (log.Count == 0) return;

            var rows = new List<string>(log.Count);
            foreach (var w in log)
            {
                rows.Add(Csv.Line(
                    $"{w.runId},{w.day},{w.hour:F1},{w.season},{w.weather},",
                    $"{w.temperature:F1},{w.cropGrowthMult:F2},{w.movementPenalty:F2}"
                ));
            }

            Csv.Append(Path.Combine(ctx.FolderPath, "weather.csv"), Header, rows);
            log.Clear();
        }

        private struct WeatherHourEntry
        {
            public int runId, day;
            public float hour;
            public string season, weather;
            public float temperature, cropGrowthMult, movementPenalty;
        }
    }
}
