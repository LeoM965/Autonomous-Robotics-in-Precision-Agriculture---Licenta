using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using AI.ML;
using UI.Utils;
using AI.Analytics;

namespace UI.Menus.Tabs
{
    public class MLModelDashboardTab : BaseDashboardTab
    {
        private string trainStatus = "Așteaptă acțiune...";
        private bool isTraining = false;
        private string trainingOutput = "";
        private float trainingDuration = 0f;

        public override void DrawTab(float x, float y, UITheme theme)
        {
            // Draw model status
            bool isLoaded = CropMLPredictor.IsLoaded;
            string statusStr = isLoaded ? "<color=green>Activ (Model Încărcat)</color>" : "<color=red>Inactiv (Model Lipsă)</color>";

            GUI.Label(new Rect(x, y, 400, 20), $"Status Model ML: {statusStr}", theme.Value);
            y += 30;

            // Draw CSV data stats
            string simDataPath = Path.Combine(Application.dataPath, "..", "Exported_SimData");
            int runCount = 0;
            int fileCount = 0;
            if (Directory.Exists(simDataPath))
            {
                var runDirs = Directory.GetDirectories(simDataPath, "*");
                foreach (var dir in runDirs)
                {
                    // Exclude potential non-run folders if they don't contain parcels
                    var files = Directory.GetFiles(dir, "parcels_day_*.csv");
                    if (files.Length > 0)
                    {
                        runCount++;
                        fileCount += files.Length;
                    }
                }
            }

            GUI.Label(new Rect(x, y, 400, 20), $"Sesiuni de date cu mostre: {runCount}", theme.Label);
            y += 20;
            GUI.Label(new Rect(x, y, 400, 20), $"Fișiere CSV de antrenament: {fileCount}", theme.Label);
            y += 30;

            // Row with buttons
            if (isTraining)
            {
                GUI.enabled = false;
                GUI.Button(new Rect(x, y, 200, 30), "Se antrenează...", theme.Label);
                GUI.enabled = true;
            }
            else
            {
                if (GUI.Button(new Rect(x, y, 200, 30), "Antrenează Model ML", theme.Value))
                {
                    StartTraining();
                }
            }

            // Save Snapshot button
            if (GUI.Button(new Rect(x + 220, y, 220, 30), "Salvează Snapshot Date Acum", theme.Value))
            {
                var exporter = Object.FindFirstObjectByType<DataExporter>();
                if (exporter != null)
                {
                    exporter.SaveDailySnapshot();
                    trainStatus = "Snapshot salvat cu succes!";
                }
                else
                {
                    trainStatus = "Eroare: Nu s-a găsit DataExporter în scenă.";
                }
            }

            y += 45;

            // Status output
            GUI.Label(new Rect(x, y, 700, 20), $"Status: {trainStatus}", theme.Label);
            y += 25;

            if (!string.IsNullOrEmpty(trainingOutput))
            {
                GUI.Label(new Rect(x, y, 700, 20), "Output consolă antrenare (ultimul run):", theme.Value);
                y += 20;

                // Draw a simple box for output
                MapHelper.DrawBox(new Rect(x, y, 700, 180), new Color(0, 0, 0, 0.25f));
                
                // Show output (truncate if too long so it doesn't overflow GUI)
                string outputToShow = trainingOutput;
                if (outputToShow.Length > 800)
                {
                    outputToShow = "..." + outputToShow.Substring(outputToShow.Length - 800);
                }
                
                GUI.Label(new Rect(x + 10, y + 5, 680, 170), outputToShow, theme.Label);
            }
        }

        private async void StartTraining()
        {
            isTraining = true;
            trainStatus = "Se rulează scriptul Python...";
            trainingOutput = "";
            float startTime = Time.realtimeSinceStartup;

            string pythonScript = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "scripts_python", "train_crop_model.py"));

            if (!File.Exists(pythonScript))
            {
                // Fallback for built player running inside Demo/ folder
                pythonScript = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "scripts_python", "train_crop_model.py"));
            }

            if (!File.Exists(pythonScript))
            {
                trainStatus = "Eroare: train_crop_model.py nu a fost găsit.";
                isTraining = false;
                UnityEngine.Debug.LogError($"[MLModelDashboardTab] Nu s-a găsit scriptul la calea: {pythonScript}");
                return;
            }

            string simDataPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Exported_SimData"));
            string outputFile = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "Resources", "MLCropModel.json"))
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "MLCropModel.json"));

            try
            {
                var result = await Task.Run(() => RunPythonScript(pythonScript, simDataPath, outputFile));
                trainingDuration = Time.realtimeSinceStartup - startTime;

                trainingOutput = result.output;

                if (result.exitCode == 0)
                {
                    trainStatus = $"Succes! Antrenare finalizată în {trainingDuration:F1}s.";
                    // Reload ML model (forceReload: true)
                    bool reloaded = CropMLPredictor.Load(true);
                    if (reloaded)
                    {
                        trainStatus += " Model reîncărcat cu succes.";
                    }
                    else
                    {
                        trainStatus += " Modelul nu a putut fi reîncărcat (probabil a fost șters din lipsă de date).";
                    }
                }
                else
                {
                    if (result.exitCode == 9009 || result.output.Contains("Python was not found") || result.output.Contains("python was not found"))
                    {
                        trainStatus = "Eroare: Python nu este instalat pe acest PC!";
                        trainingOutput = "Pentru a folosi antrenarea, te rugăm să instalezi Python 3 de pe site-ul oficial (python.org) sau din Microsoft Store.\n\n" +
                                         "IMPORTANT: În timpul instalării, bifează căsuța \"Add Python to PATH\".\n" +
                                         "După instalare, rulează în CMD/PowerShell:\n" +
                                         "pip install pandas numpy scikit-learn\n\n" +
                                         "Detalii eroare:\n" + result.output;
                    }
                    else
                    {
                        trainStatus = $"Eroare! Cod ieșire script: {result.exitCode}. Verifică dependințele.";
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                trainStatus = "Eroare: Python nu este instalat sau adăugat în PATH!";
                trainingOutput = "Pentru a folosi re-antrenarea, asigură-te că Python 3 este instalat și configurat în variabilele de mediu ale acestui PC.\nDetalii: " + ex.Message;
            }
            catch (System.Exception ex)
            {
                trainStatus = $"Eroare la pornirea scriptului: {ex.Message}";
                UnityEngine.Debug.LogError($"[MLModelDashboardTab] Eroare rulare script: {ex}");
            }

            isTraining = false;
        }

        private string ResolvePythonPath()
        {
            // Cautam in locatiile implicite de instalare de pe Windows
            string localAppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            
            // Verificam versiuni comune de Python (3.13, 3.12, 3.11, etc.)
            string[] versions = { "Python313", "Python312", "Python311", "Python310", "Python39", "Python38" };
            foreach (var ver in versions)
            {
                string path = Path.Combine(localAppData, "Programs", "Python", ver, "python.exe");
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Fallback pe variabila globala PATH
            return "python";
        }

        private (int exitCode, string output) RunPythonScript(string scriptPath, string dataDir, string outputFile)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = ResolvePythonPath();
            start.Arguments = $"\"{scriptPath}\" \"{dataDir}\" \"{outputFile}\"";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.CreateNoWindow = true;

            using (Process process = Process.Start(start))
            {
                // Citește asincron din stderr pentru a preveni blocarea buffer-ului (deadlock)
                var errorReader = Task.Run(() => process.StandardError.ReadToEnd());
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string error = errorReader.Result;
                string fullLog = output;
                if (!string.IsNullOrEmpty(error))
                {
                    fullLog += "\nERORI:\n" + error;
                }

                return (process.ExitCode, fullLog);
            }
        }
    }
}
