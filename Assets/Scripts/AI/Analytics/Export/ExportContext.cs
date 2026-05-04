namespace AI.Analytics.Export
{
    /// <summary>
    /// Date comune partajate intre toti exporterii la fiecare snapshot zilnic.
    /// Elimina accesul repetat la TimeManager/WeatherSystem din fiecare exporter.
    /// </summary>
    public readonly struct ExportContext
    {
        public readonly int RunId;
        public readonly int Day;
        public readonly string FolderPath;
        public readonly string Season;
        public readonly string WeatherCondition;
        public readonly float Temperature;

        public ExportContext(int runId, int day, string folderPath)
        {
            RunId = runId;
            Day = day;
            FolderPath = folderPath;

            var tm = TimeManager.Instance;
            var ws = Weather.Components.WeatherSystem.Instance;

            Season = tm != null ? tm.GetCurrentSeason().ToString() : "";
            WeatherCondition = ws != null ? ws.CurrentWeather.ToString() : "";
            Temperature = ws != null ? ws.CurrentTemperature : 0f;
        }
    }
}
