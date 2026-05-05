using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using SaveSystem;

namespace UI.MainMenu
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string simulationScene = "CampFertil";

        // Paleta de culori — tonuri naturale, nu neon
        private static readonly Color BG_TOP      = new Color(0.05f, 0.07f, 0.12f);
        private static readonly Color BG_BOT      = new Color(0.02f, 0.03f, 0.06f);
        private static readonly Color CARD        = new Color(0.08f, 0.10f, 0.15f, 0.92f);
        private static readonly Color CARD_EDGE   = new Color(0.18f, 0.22f, 0.30f, 0.35f);
        private static readonly Color ACCENT      = new Color(0.30f, 0.65f, 0.50f);    // verde natural
        private static readonly Color ACCENT_LIT  = new Color(0.35f, 0.75f, 0.55f);
        private static readonly Color TXT_MAIN    = new Color(0.90f, 0.91f, 0.93f);
        private static readonly Color TXT_DIM     = new Color(0.50f, 0.54f, 0.62f);
        private static readonly Color TXT_MID     = new Color(0.68f, 0.72f, 0.78f);
        private static readonly Color BTN_SEC     = new Color(0.14f, 0.16f, 0.22f);
        private static readonly Color BTN_SEC_H   = new Color(0.20f, 0.22f, 0.30f);
        private static readonly Color BTN_QUIT_BG = new Color(0.18f, 0.10f, 0.10f);
        private static readonly Color BTN_QUIT_H  = new Color(0.30f, 0.12f, 0.12f);
        private static readonly Color SEPARATOR   = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color OVERLAY_BG  = new Color(0.02f, 0.03f, 0.06f, 0.85f);
        private static readonly Color DEL_BG      = new Color(0.45f, 0.15f, 0.15f);
        private static readonly Color DEL_H       = new Color(0.60f, 0.18f, 0.18f);

        private Transform rootTransform;
        private GameObject rootCanvas;
        private GameObject savesPanel;

        void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            Build();
        }

        private void Build()
        {
            // ── Canvas ──
            var root = new GameObject("MenuCanvas");
            var c = root.AddComponent<UnityEngine.Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();
            rootCanvas = root;
            rootTransform = root.transform;

            // ── Background ──
            Stretch(root.transform, "BG", BG_TOP);

            // ── Card ──
            var card = new GameObject("Card");
            card.transform.SetParent(root.transform, false);
            var cRt = card.AddComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(580, 750);
            cRt.anchoredPosition = new Vector2(0, 20);
            card.AddComponent<Image>().color = CARD;
            var ol = card.AddComponent<Outline>();
            ol.effectColor = CARD_EDGE;
            ol.effectDistance = new Vector2(1, 1);

            float y = -35f;

            // ── Logo ──
            var logoTex = Resources.Load<Texture2D>("ASE_Logo");
            if (logoTex != null)
            {
                var logo = new GameObject("Logo");
                logo.transform.SetParent(card.transform, false);
                var lRt = Anchor(logo, 0.5f, 1f, 140, 108);
                lRt.anchoredPosition = new Vector2(0, y);
                var lImg = logo.AddComponent<Image>();
                lImg.sprite = Sprite.Create(logoTex,
                    new Rect(0, 0, logoTex.width, logoTex.height), Vector2.one * 0.5f);
                lImg.preserveAspect = true;
                lImg.raycastTarget = false;
                y -= 125;
            }

            // ── Titlu ──
            var title = Label(card.transform, "AgroBot", 48, FontStyles.Bold, TXT_MAIN, y, 54);
            title.characterSpacing = 6;
            y -= 60;

            Label(card.transform, "Simulator agricol multi-agent", 16, FontStyles.Normal, ACCENT, y, 24);
            y -= 28;

            Label(card.transform, "Lucrare de licență  ·  ASE București  ·  2026", 12, FontStyles.Normal, TXT_DIM, y, 18);
            y -= 35;

            // ── Separator ──
            HLine(card.transform, 200, y);
            y -= 30;

            // ── Nume simulare (auto-increment) ──
            string nextRun = GetNextRunName();
            Label(card.transform, $"Simulare nouă:  {nextRun}", 14, FontStyles.Normal, TXT_MID, y, 22);
            y -= 35;

            // ── Start simulare nouă ──
            MakeButton(card.transform, $"Pornește  ·  {nextRun}", ACCENT, ACCENT_LIT, Color.black, y, 50, 16, () =>
            {
                SimSaveManager.LastSaveName = nextRun;
                SimLoader.ShouldLoadSave = false;
                SceneManager.LoadScene(simulationScene);
            });
            y -= 60;

            // ── Buton Simulări Salvate ──
            var saves = SimSaveManager.GetSaveNames();
            string savesLabel = saves.Length > 0
                ? $"Simulări Salvate  ({saves.Length})"
                : "Simulări Salvate";

            MakeButton(card.transform, savesLabel, BTN_SEC, BTN_SEC_H, ACCENT, y, 44, 14, () =>
            {
                ShowSavesPanel();
            });
            y -= 58;

            // ── Ieșire ──
            MakeButton(card.transform, "Ieșire", BTN_QUIT_BG, BTN_QUIT_H, TXT_DIM, y, 40, 13, () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // ── Footer ──
            var fGo = new GameObject("Footer");
            fGo.transform.SetParent(root.transform, false);
            var fRt = fGo.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0.5f, 0);
            fRt.anchorMax = new Vector2(0.5f, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.sizeDelta = new Vector2(800, 55);
            fRt.anchoredPosition = new Vector2(0, 18);
            var ft = fGo.AddComponent<TextMeshProUGUI>();
            ft.text = "Academia de Studii Economice din București  ·  Facultatea CSIE\nMircea Ștefăniță-Leonard  ·  Coordonator: Lector. dr. Zurini Mădălina";
            ft.fontSize = 13;
            ft.alignment = TextAlignmentOptions.Center;
            ft.color = TXT_MID;
            ft.raycastTarget = false;

            // ── Versiune ──
            var vGo = new GameObject("Ver");
            vGo.transform.SetParent(root.transform, false);
            var vRt = vGo.AddComponent<RectTransform>();
            vRt.anchorMin = vRt.anchorMax = new Vector2(1, 0);
            vRt.pivot = new Vector2(1, 0);
            vRt.sizeDelta = new Vector2(160, 18);
            vRt.anchoredPosition = new Vector2(-14, 8);
            var vt = vGo.AddComponent<TextMeshProUGUI>();
            vt.text = "v1.0  ·  Unity 6";
            vt.fontSize = 11;
            vt.alignment = TextAlignmentOptions.Right;
            vt.color = TXT_DIM;
            vt.raycastTarget = false;
        }

        // ────────────────────────────
        //  Saves Panel
        // ────────────────────────────

        private void ShowSavesPanel()
        {
            if (savesPanel != null) Destroy(savesPanel);

            savesPanel = new GameObject("SavesPanel");
            savesPanel.transform.SetParent(rootTransform, false);

            // Overlay fullscreen
            var overlayRt = savesPanel.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            var overlayImg = savesPanel.AddComponent<Image>();
            overlayImg.color = OVERLAY_BG;

            // Block clicks behind
            savesPanel.AddComponent<Button>().onClick.AddListener(() => { /* block */ });

            // Card
            var panelCard = new GameObject("PanelCard");
            panelCard.transform.SetParent(savesPanel.transform, false);
            var pcRt = panelCard.AddComponent<RectTransform>();
            pcRt.anchorMin = pcRt.anchorMax = new Vector2(0.5f, 0.5f);
            pcRt.sizeDelta = new Vector2(620, 580);
            panelCard.AddComponent<Image>().color = CARD;
            var pcOl = panelCard.AddComponent<Outline>();
            pcOl.effectColor = CARD_EDGE;
            pcOl.effectDistance = new Vector2(1, 1);

            float y = -25f;

            // Title
            Label(panelCard.transform, "Simulări Salvate", 28, FontStyles.Bold, TXT_MAIN, y, 36);
            y -= 45;
            HLine(panelCard.transform, 500, y);
            y -= 20;

            var saves = SimSaveManager.GetSaveNames();

            if (saves.Length == 0)
            {
                Label(panelCard.transform, "Nu există simulări salvate.", 15, FontStyles.Italic, TXT_DIM, y, 24);
                y -= 40;
            }
            else
            {
                // Scroll area
                var scrollGo = new GameObject("Scroll");
                scrollGo.transform.SetParent(panelCard.transform, false);
                var scrollRt = scrollGo.AddComponent<RectTransform>();
                scrollRt.anchorMin = new Vector2(0.5f, 1f);
                scrollRt.anchorMax = new Vector2(0.5f, 1f);
                scrollRt.pivot = new Vector2(0.5f, 1f);
                float scrollH = 380f;
                scrollRt.sizeDelta = new Vector2(560, scrollH);
                scrollRt.anchoredPosition = new Vector2(0, y);

                var scrollImg = scrollGo.AddComponent<Image>();
                scrollImg.color = new Color(0, 0, 0, 0.01f);

                var mask = scrollGo.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                var scrollRect = scrollGo.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 30f;

                // Content
                var contentGo = new GameObject("Content");
                contentGo.transform.SetParent(scrollGo.transform, false);
                var contentRt = contentGo.AddComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0, 1);
                contentRt.anchorMax = new Vector2(1, 1);
                contentRt.pivot = new Vector2(0.5f, 1f);

                float itemH = 80f;
                contentRt.sizeDelta = new Vector2(0, saves.Length * itemH);
                scrollRect.content = contentRt;

                for (int i = 0; i < saves.Length; i++)
                {
                    string sName = saves[i];
                    var info = SimSaveManager.PeekSave(sName);
                    float iy = -(i * itemH);

                    // Item row BG
                    var rowGo = new GameObject("Row");
                    rowGo.transform.SetParent(contentGo.transform, false);
                    var rowRt = rowGo.AddComponent<RectTransform>();
                    rowRt.anchorMin = new Vector2(0, 1);
                    rowRt.anchorMax = new Vector2(1, 1);
                    rowRt.pivot = new Vector2(0.5f, 1f);
                    rowRt.sizeDelta = new Vector2(0, itemH);
                    rowRt.anchoredPosition = new Vector2(0, iy);
                    rowGo.AddComponent<Image>().color = (i % 2 == 0)
                        ? new Color(0.06f, 0.08f, 0.12f, 0.5f)
                        : new Color(0.08f, 0.10f, 0.15f, 0.3f);

                    // Save name
                    var nameGo = new GameObject("Name");
                    nameGo.transform.SetParent(rowGo.transform, false);
                    var nameRt = nameGo.AddComponent<RectTransform>();
                    nameRt.anchorMin = new Vector2(0, 0.5f);
                    nameRt.anchorMax = new Vector2(0, 0.5f);
                    nameRt.pivot = new Vector2(0, 0.5f);
                    nameRt.sizeDelta = new Vector2(300, 24);
                    nameRt.anchoredPosition = new Vector2(15, 12);
                    var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
                    nameTxt.text = sName;
                    nameTxt.fontSize = 16;
                    nameTxt.fontStyle = FontStyles.Bold;
                    nameTxt.color = TXT_MAIN;
                    nameTxt.raycastTarget = false;

                    // Details
                    if (info != null)
                    {
                        var detGo = new GameObject("Det");
                        detGo.transform.SetParent(rowGo.transform, false);
                        var detRt = detGo.AddComponent<RectTransform>();
                        detRt.anchorMin = new Vector2(0, 0.5f);
                        detRt.anchorMax = new Vector2(0, 0.5f);
                        detRt.pivot = new Vector2(0, 0.5f);
                        detRt.sizeDelta = new Vector2(350, 20);
                        detRt.anchoredPosition = new Vector2(15, -12);
                        var detTxt = detGo.AddComponent<TextMeshProUGUI>();
                        string weatherInfo = !string.IsNullOrEmpty(info.weatherType)
                            ? $"  ·  {info.temperature:F0}°C {info.weatherType}"
                            : "";
                        detTxt.text = $"Ziua {info.dayNumber}  ·  {info.parcels.Count} parcele{weatherInfo}  ·  {info.savedAt}";
                        detTxt.fontSize = 12;
                        detTxt.color = TXT_DIM;
                        detTxt.raycastTarget = false;
                    }

                    // Load button
                    MakeRowButton(rowGo.transform, "Încarcă", ACCENT, ACCENT_LIT, Color.black,
                        new Vector2(-110, 0), new Vector2(90, 32), 13, () =>
                    {
                        SimSaveManager.LastSaveName = sName;
                        SimLoader.ShouldLoadSave = true;
                        SceneManager.LoadScene(simulationScene);
                    });

                    // Delete button
                    MakeRowButton(rowGo.transform, "Șterge", DEL_BG, DEL_H, TXT_MAIN,
                        new Vector2(-15, 0), new Vector2(80, 32), 13, () =>
                    {
                        SimSaveManager.DeleteSave(sName);
                        Rebuild(); // rebuild entire menu with updated count
                    });
                }

                y -= scrollH + 10;
            }

            // Close button
            y = -540f;
            MakeButton(panelCard.transform, "Închide", BTN_SEC, BTN_SEC_H, TXT_MID, y, 36, 13, () =>
            {
                Rebuild(); // rebuild to update save count on main card
            });
        }

        // ────────────────────────────
        //  Rebuild
        // ────────────────────────────

        private void Rebuild()
        {
            if (savesPanel != null) { Destroy(savesPanel); savesPanel = null; }
            if (rootCanvas != null) Destroy(rootCanvas);
            Build();
        }

        // ────────────────────────────
        //  Auto-increment run name
        // ────────────────────────────

        private string GetNextRunName()
        {
            var saves = SimSaveManager.GetSaveNames();
            int max = 0;
            foreach (var s in saves)
            {
                if (s.StartsWith("Run_") && s.Length == 7)
                {
                    if (int.TryParse(s.Substring(4), out int num) && num > max)
                        max = num;
                }
            }
            return $"Run_{(max + 1):D3}";
        }

        // ────────────────────────────
        //  Helpers
        // ────────────────────────────

        private void Stretch(Transform p, string n, Color c)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            go.AddComponent<Image>().color = c;
        }

        private RectTransform Anchor(GameObject go, float ax, float ay, float w, float h)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private TextMeshProUGUI Label(Transform p, string text, float sz, FontStyles style, Color col, float yPos, float h)
        {
            var go = new GameObject("L");
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(500, h);
            rt.anchoredPosition = new Vector2(0, yPos);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = sz;
            t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.color = col;
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            return t;
        }

        private void HLine(Transform p, float width, float yPos)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 1);
            rt.anchoredPosition = new Vector2(0, yPos);
            go.AddComponent<Image>().color = SEPARATOR;
        }

        private void MakeButton(Transform p, string text, Color bg, Color hover, Color txtCol, float yPos, float h, float fSize, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(360, h);
            rt.anchoredPosition = new Vector2(0, yPos);

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = bg;
            cb.highlightedColor = hover;
            cb.pressedColor = bg * 0.75f;
            cb.selectedColor = bg;
            cb.fadeDuration = 0.1f;
            btn.colors = cb;
            if (action != null) btn.onClick.AddListener(action);

            var lbl = new GameObject("T");
            lbl.transform.SetParent(go.transform, false);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = txtCol;
            t.raycastTarget = false;
        }

        private void MakeRowButton(Transform p, string text, Color bg, Color hover, Color txtCol,
            Vector2 anchoredPos, Vector2 size, float fSize, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("RBtn");
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = bg;
            cb.highlightedColor = hover;
            cb.pressedColor = bg * 0.75f;
            cb.selectedColor = bg;
            cb.fadeDuration = 0.1f;
            btn.colors = cb;
            if (action != null) btn.onClick.AddListener(action);

            var lbl = new GameObject("T");
            lbl.transform.SetParent(go.transform, false);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = txtCol;
            t.raycastTarget = false;
        }
    }
}
