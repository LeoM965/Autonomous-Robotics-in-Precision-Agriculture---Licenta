using UnityEngine;
using System.IO;
using AI.Analytics.Export;

namespace AI.Analytics
{
    /// <summary>
    /// Orchestrator central — gestioneaza ciclul de viata al exporturilor CSV.
    /// Fiecare tip de export este delegat catre o clasa dedicata (SRP).
    /// </summary>
    public class DataExporter : MonoBehaviour
    {
        private int runId;
        private string folderPath;

        // Exporteri specializati
        private MetadataExporter metadata;
        private RobotExporter robots;
        private ParcelExporter parcels;
        private DecisionExporter decisions;
        private EconomyExporter economy;
        private HistoryExporter history;
        private WeatherExporter weather;

        private const string RunCounterFile = "sim_run_counter.txt";

        // ──────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
            string basePath = Path.Combine(Application.dataPath, "..", "Exported_SimData");
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            // Use simulation name from MainMenu if available, otherwise auto-increment
            string simName = null;
            if (!string.IsNullOrEmpty(SaveSystem.SimSaveManager.LastSaveName))
                simName = SaveSystem.SimSaveManager.LastSaveName;

            if (!string.IsNullOrEmpty(simName))
            {
                folderPath = Path.Combine(basePath, simName);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                runId = LoadOrCreateLocalRunId(folderPath, basePath);
            }
            else
            {
                runId = LoadAndIncrementRunCounter(basePath);
                folderPath = Path.Combine(basePath, $"Run_{runId:D3}");
            }

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            CreateExporters();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += SaveDailySnapshot;
                TimeManager.Instance.OnHourChanged += OnHourChanged;
            }

            // Exportam metadata cu un mic delay (dupa Awake-urile altor componente)
            Invoke(nameof(ExportMetadata), 0.5f);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= SaveDailySnapshot;
                TimeManager.Instance.OnHourChanged -= OnHourChanged;
            }

            // Salvam datele meteo ramase la inchidere (zi partiala)
            if (weather.HasPendingData)
                weather.Export(CreateContext(TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0));
        }

        // ──────────────────────────────────────────────
        // Setup
        // ──────────────────────────────────────────────

        private void CreateExporters()
        {
            metadata  = new MetadataExporter();
            robots    = new RobotExporter();
            parcels   = new ParcelExporter();
            decisions = new DecisionExporter();
            economy   = new EconomyExporter();
            history   = new HistoryExporter();
            weather   = new WeatherExporter();
        }

        // ──────────────────────────────────────────────
        // Evenimente
        // ──────────────────────────────────────────────

        private void OnHourChanged(float hour) => weather.LogHour(runId, hour);

        private void ExportMetadata() => metadata.Export(CreateContext(0));

        [ContextMenu("Export Snapshot")]
        public void SaveDailySnapshot()
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.currentDay - 1 : 0;
            var ctx = CreateContext(day);

            robots.Export(ctx);
            parcels.Export(ctx);
            decisions.Export(ctx);
            economy.Export(ctx);
            weather.Export(ctx);
            history.Export(ctx);

            Debug.Log($"<color=cyan><b>[DataExporter]</b> Run #{runId} | 6 CSV-uri exportate pentru Ziua {day}.</color>");
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private ExportContext CreateContext(int day) => new ExportContext(runId, day, folderPath);

        private int LoadOrCreateLocalRunId(string folder, string basePath)
        {
            string localFile = Path.Combine(folder, "run_id.txt");
            if (File.Exists(localFile) && int.TryParse(File.ReadAllText(localFile).Trim(), out int id))
                return id;
            int newId = LoadAndIncrementRunCounter(basePath);
            File.WriteAllText(localFile, newId.ToString());
            return newId;
        }

        private int LoadAndIncrementRunCounter(string basePath)
        {
            string counterPath = Path.Combine(basePath, RunCounterFile);
            int id = 1;

            if (File.Exists(counterPath))
            {
                if (int.TryParse(File.ReadAllText(counterPath).Trim(), out int existing))
                    id = existing + 1;
            }

            File.WriteAllText(counterPath, id.ToString());
            return id;
        }
    }
}
