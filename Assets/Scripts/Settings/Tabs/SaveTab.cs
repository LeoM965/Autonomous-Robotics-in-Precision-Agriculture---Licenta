using UnityEngine;
using SaveSystem;

namespace Settings.Tabs
{
    public class SaveTab : ISettingsTab
    {
        public string Title => "Salvare";

        private string saveName = "Simulare_1";
        private string[] existingSaves;
        private string statusMessage = "";
        private float statusTimer;
        private Vector2 scrollPos;

        public SaveTab()
        {
            RefreshList();
        }

        private void RefreshList()
        {
            existingSaves = SimSaveManager.GetSaveNames();
        }

        public void Draw(Rect area, UITheme theme)
        {
            float x = 0, y = 0;

            // ── Salvare nouă ──
            GUI.Label(new Rect(x, y, 250, 20), "SALVARE SIMULARE", theme.Title);
            y += 35;

            GUI.Label(new Rect(x, y, 120, 20), "Nume salvare:", theme.Label);
            saveName = GUI.TextField(new Rect(x + 125, y, 200, 22), saveName, 30, theme.Input);
            y += 30;

            if (GUI.Button(new Rect(x, y, 180, 28), "Salvează", theme.Button))
            {
                if (!string.IsNullOrWhiteSpace(saveName) && SimSaveManager.Instance != null)
                {
                    SimSaveManager.Instance.Save(saveName.Trim());
                    statusMessage = $"Salvat: \"{saveName.Trim()}\"";
                    statusTimer = 3f;
                    RefreshList();
                }
            }

            // Status
            if (statusTimer > 0)
            {
                statusTimer -= Time.deltaTime;
                GUI.Label(new Rect(x + 190, y + 4, 250, 20), statusMessage, theme.Good);
            }
            y += 45;

            // ── Lista salvărilor ──
            GUI.Label(new Rect(x, y, 250, 20), "SALVĂRI EXISTENTE", theme.Title);
            y += 30;

            if (existingSaves == null || existingSaves.Length == 0)
            {
                GUI.Label(new Rect(x, y, 300, 20), "Nicio salvare găsită.", theme.Label);
                return;
            }

            float listH = area.height - y - 10;
            float itemH = 55f;

            scrollPos = GUI.BeginScrollView(
                new Rect(x, y, area.width, listH),
                scrollPos,
                new Rect(0, 0, area.width - 20, existingSaves.Length * itemH)
            );

            for (int i = 0; i < existingSaves.Length; i++)
            {
                float iy = i * itemH;
                string name = existingSaves[i];
                var info = SimSaveManager.PeekSave(name);

                // Numele salvării
                GUI.Label(new Rect(5, iy + 2, 250, 20), name, theme.Value);

                // Detalii
                if (info != null)
                {
                    string details = $"Ziua {info.dayNumber}  ·  {info.parcels.Count} parcele  ·  {info.savedAt}";
                    GUI.Label(new Rect(5, iy + 20, 350, 18), details, theme.Label);
                }

                // Buton încarcă
                if (GUI.Button(new Rect(area.width - 175, iy + 5, 75, 22), "Încarcă", theme.Button))
                {
                    if (SimSaveManager.Instance != null)
                    {
                        SimSaveManager.Instance.Load(name);
                        statusMessage = $"Încărcat: \"{name}\"";
                        statusTimer = 3f;
                    }
                }

                // Buton șterge
                if (GUI.Button(new Rect(area.width - 90, iy + 5, 65, 22), "Șterge", theme.Bad))
                {
                    SimSaveManager.DeleteSave(name);
                    RefreshList();
                    statusMessage = $"Șters: \"{name}\"";
                    statusTimer = 3f;
                }

                // Separator
                GUI.Box(new Rect(0, iy + itemH - 2, area.width - 20, 1), "", GUIStyle.none);
            }

            GUI.EndScrollView();
        }
    }
}
