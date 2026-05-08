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

        // Palette — deep indigo + teal + warm gold
        static readonly Color BG        = new Color(0.025f, 0.028f, 0.055f);
        static readonly Color LEFT_BG   = new Color(0.04f, 0.045f, 0.085f, 0.97f);
        static readonly Color RIGHT_BG  = new Color(0.055f, 0.06f, 0.10f, 0.93f);
        static readonly Color ACCENT    = new Color(0.16f, 0.72f, 0.64f);      // teal
        static readonly Color ACCENT_LT = new Color(0.22f, 0.82f, 0.72f);
        static readonly Color GOLD      = new Color(0.85f, 0.68f, 0.32f);      // warm gold
        static readonly Color GOLD_DIM  = new Color(0.55f, 0.44f, 0.22f);
        static readonly Color TXT       = new Color(0.92f, 0.93f, 0.96f);
        static readonly Color TXT_DIM   = new Color(0.40f, 0.43f, 0.52f);
        static readonly Color TXT_MID   = new Color(0.62f, 0.66f, 0.74f);
        static readonly Color BTN_SEC   = new Color(0.08f, 0.09f, 0.15f);
        static readonly Color BTN_SEC_H = new Color(0.13f, 0.15f, 0.22f);
        static readonly Color BTN_Q     = new Color(0.15f, 0.07f, 0.07f);
        static readonly Color BTN_Q_H   = new Color(0.25f, 0.10f, 0.10f);
        static readonly Color SEP       = new Color(1f, 1f, 1f, 0.04f);
        static readonly Color BORDER    = new Color(0.16f, 0.72f, 0.64f, 0.06f);
        static readonly Color OVERLAY   = new Color(0.015f, 0.02f, 0.04f, 0.92f);
        static readonly Color DEL_BG    = new Color(0.50f, 0.16f, 0.16f);
        static readonly Color DEL_H     = new Color(0.65f, 0.20f, 0.20f);
        static readonly Color STAT_BG   = new Color(0.04f, 0.05f, 0.08f, 0.70f);

        Transform _root;
        GameObject _rootCanvas, _savesPanel;

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

        void Build()
        {
            var root = new GameObject("MenuCanvas");
            var cv = root.AddComponent<UnityEngine.Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = root.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();
            _rootCanvas = root;
            _root = root.transform;

            // Full BG
            StretchImg(root.transform, "BG", BG);

            // ════════════════════════════════
            //  Main container — centered horizontal split
            // ════════════════════════════════
            var container = Go("Container", root.transform);
            var contRt = container.AddComponent<RectTransform>();
            contRt.anchorMin = contRt.anchorMax = new Vector2(0.5f, 0.5f);
            contRt.sizeDelta = new Vector2(960, 620);
            contRt.anchoredPosition = new Vector2(0, 10);


            // ── LEFT PANEL (Branding) ──
            var left = Go("Left", container.transform);
            var leftRt = left.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0.42f, 1);
            leftRt.sizeDelta = Vector2.zero;
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;
            left.AddComponent<Image>().color = LEFT_BG;
            var lOl = left.AddComponent<Outline>();
            lOl.effectColor = BORDER; lOl.effectDistance = new Vector2(1, 1);

            // Vertical accent stripe on left edge of left panel
            var stripe = Go("Stripe", left.transform);
            var strRt = stripe.AddComponent<RectTransform>();
            strRt.anchorMin = new Vector2(0, 0.05f); strRt.anchorMax = new Vector2(0, 0.95f);
            strRt.pivot = new Vector2(0, 0.5f); strRt.sizeDelta = new Vector2(3, 0);
            strRt.anchoredPosition = new Vector2(0, 0);
            stripe.AddComponent<Image>().color = ACCENT;

            // Vertical divider accent between panels
            var divider = Go("Divider", container.transform);
            var divRt = divider.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.42f, 0.08f); divRt.anchorMax = new Vector2(0.42f, 0.92f);
            divRt.pivot = new Vector2(0.5f, 0.5f); divRt.sizeDelta = new Vector2(1, 0);
            divider.AddComponent<Image>().color = new Color(0.16f, 0.72f, 0.64f, 0.25f);

            // Logo
            float ly = -35f;

            // Logo
            var logoTex = Resources.Load<Texture2D>("ASE_Logo");
            if (logoTex != null)
            {
                var logo = Go("Logo", left.transform);
                var logoRt = logo.AddComponent<RectTransform>();
                logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 1f);
                logoRt.pivot = new Vector2(0.5f, 1f);
                logoRt.sizeDelta = new Vector2(120, 94);
                logoRt.anchoredPosition = new Vector2(0, ly);
                var lImg = logo.AddComponent<Image>();
                lImg.sprite = Sprite.Create(logoTex, new Rect(0, 0, logoTex.width, logoTex.height), Vector2.one * 0.5f);
                lImg.preserveAspect = true;
                lImg.raycastTarget = false;
                ly -= 108;
            }

            // Title
            var ttl = Lbl(left.transform, "AgroBot", 46, FontStyles.Bold, TXT, ly, 54, 340);
            ttl.characterSpacing = 6;
            ly -= 50;

            Lbl(left.transform, "Simulator agricol\nmulti-agent", 16, FontStyles.Italic, ACCENT, ly, 42, 300);
            ly -= 50;

            // Gold decorative line
            var goldLine = Go("GoldLine", left.transform);
            var glRt = goldLine.AddComponent<RectTransform>();
            glRt.anchorMin = glRt.anchorMax = new Vector2(0.5f, 1f);
            glRt.pivot = new Vector2(0.5f, 0.5f);
            glRt.sizeDelta = new Vector2(60, 2);
            glRt.anchoredPosition = new Vector2(0, ly);
            goldLine.AddComponent<Image>().color = GOLD;
            ly -= 22;

            // Info lines
            Lbl(left.transform, "Lucrare de licență", 13, FontStyles.Normal, TXT_MID, ly, 18, 280);
            ly -= 20;
            Lbl(left.transform, "ASE București · CSIE · 2026", 12, FontStyles.Normal, GOLD_DIM, ly, 18, 280);
            ly -= 30;

            // Stats mini-boxes
            var saves = SimSaveManager.GetSaveNames();
            string nextRun = GetNextRunName();

            var statsRow = Go("Stats", left.transform);
            var statsRt = statsRow.AddComponent<RectTransform>();
            statsRt.anchorMin = statsRt.anchorMax = new Vector2(0.5f, 1f);
            statsRt.pivot = new Vector2(0.5f, 1f);
            statsRt.sizeDelta = new Vector2(300, 56);
            statsRt.anchoredPosition = new Vector2(0, ly);

            StatBox(statsRow.transform, 0f, saves.Length.ToString(), "salvări", 150);
            StatBox(statsRow.transform, 150f, nextRun.Replace("Run_", "#"), "următoarea", 150);

            ly -= 70;

            // Coordonator
            Lbl(left.transform, "Coordonator", 11, FontStyles.Bold, GOLD_DIM, ly, 16, 280);
            ly -= 18;
            Lbl(left.transform, "Lect. dr. Zurini Mădălina", 13, FontStyles.Normal, TXT_MID, ly, 18, 280);

            // ── RIGHT PANEL (Actions) ──
            var right = Go("Right", container.transform);
            var rightRt = right.AddComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(0.42f, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.sizeDelta = Vector2.zero;
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;
            right.AddComponent<Image>().color = RIGHT_BG;
            var rOl = right.AddComponent<Outline>();
            rOl.effectColor = BORDER; rOl.effectDistance = new Vector2(1, 1);

            float ry = -40f;

            Lbl(right.transform, "Panou de control", 28, FontStyles.Bold, TXT, ry, 36, 460);
            ry -= 36;

            // Gold accent under title
            var titleAccent = Go("TitleAccent", right.transform);
            var taRt = titleAccent.AddComponent<RectTransform>();
            taRt.anchorMin = taRt.anchorMax = new Vector2(0.5f, 1f);
            taRt.pivot = new Vector2(0.5f, 0.5f);
            taRt.sizeDelta = new Vector2(50, 2);
            taRt.anchoredPosition = new Vector2(0, ry);
            titleAccent.AddComponent<Image>().color = GOLD;
            ry -= 14;
            Sep(right.transform, 400, ry);
            ry -= 28;

            // Section: New simulation
            Lbl(right.transform, "SIMULARE NOUĂ", 11, FontStyles.Bold, ACCENT, ry, 16, 460);
            ry -= 24;

            Lbl(right.transform, $"Următoarea sesiune:  {nextRun}", 14, FontStyles.Normal, TXT_MID, ry, 22, 460);
            ry -= 32;

            Btn(right.transform, $"Pornește  ·  {nextRun}", ACCENT, ACCENT_LT, new Color(0.02f, 0.04f, 0.03f),
                ry, 52, 16, 460, () =>
            {
                SimSaveManager.LastSaveName = nextRun;
                SimLoader.ShouldLoadSave = false;
                SceneManager.LoadScene(simulationScene);
            });
            ry -= 65;



            Sep(right.transform, 380, ry);
            ry -= 28;

            // Section: Saved simulations
            Lbl(right.transform, "SESIUNI ANTERIOARE", 11, FontStyles.Bold, GOLD, ry, 16, 460);
            ry -= 28;

            string savesLbl = saves.Length > 0 ? $"Simulări Salvate  ({saves.Length})" : "Simulări Salvate  (0)";
            Btn(right.transform, savesLbl, BTN_SEC, BTN_SEC_H, ACCENT, ry, 46, 14, 460, ShowSavesPanel);
            ry -= 60;

            Sep(right.transform, 380, ry);
            ry -= 28;

            // Section: System
            Lbl(right.transform, "SISTEM", 11, FontStyles.Bold, TXT_MID, ry, 16, 460);
            ry -= 28;

            Btn(right.transform, "Ieșire din aplicație", BTN_Q, BTN_Q_H, TXT_DIM, ry, 42, 13, 460, () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // ── Footer ──
            // Footer
            var fGo = Go("Footer", root.transform);
            var fRt = fGo.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0.5f, 0);
            fRt.anchorMax = new Vector2(0.5f, 0);
            fRt.pivot = new Vector2(0.5f, 0);
            fRt.sizeDelta = new Vector2(880, 30);
            fRt.anchoredPosition = new Vector2(0, 10);
            var ft = fGo.AddComponent<TextMeshProUGUI>();
            ft.text = "Mircea Ștefăniță-Leonard  ·  Academia de Studii Economice din București  ·  Facultatea CSIE";
            ft.fontSize = 11;
            ft.alignment = TextAlignmentOptions.Center;
            ft.color = TXT_DIM;
            ft.raycastTarget = false;

            // Version
            var vGo = Go("Ver", root.transform);
            var vRt = vGo.AddComponent<RectTransform>();
            vRt.anchorMin = vRt.anchorMax = new Vector2(1, 0);
            vRt.pivot = new Vector2(1, 0);
            vRt.sizeDelta = new Vector2(140, 16);
            vRt.anchoredPosition = new Vector2(-14, 8);
            var vt = vGo.AddComponent<TextMeshProUGUI>();
            vt.text = "v1.0 · Unity 6";
            vt.fontSize = 10;
            vt.alignment = TextAlignmentOptions.Right;
            vt.color = TXT_DIM;
            vt.raycastTarget = false;
        }

        // ════════════════════════════════
        //  Stat Box helper
        // ════════════════════════════════
        void StatBox(Transform p, float xOff, string val, string label, float w)
        {
            var box = Go("Stat", p);
            var rt = box.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(w - 6, 46);
            rt.anchoredPosition = new Vector2(xOff, 0);
            box.AddComponent<Image>().color = STAT_BG;

            // Accent top border on stat box
            var topBorder = Go("Border", box.transform);
            var tbRt = topBorder.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0, 1); tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(0.5f, 1); tbRt.sizeDelta = new Vector2(0, 2);
            topBorder.AddComponent<Image>().color = ACCENT;

            var vGo = Go("V", box.transform);
            var vRt = vGo.AddComponent<RectTransform>();
            vRt.anchorMin = vRt.anchorMax = new Vector2(0.5f, 1f);
            vRt.pivot = new Vector2(0.5f, 1f);
            vRt.sizeDelta = new Vector2(w - 10, 24);
            vRt.anchoredPosition = new Vector2(0, -6);
            var vT = vGo.AddComponent<TextMeshProUGUI>();
            vT.text = val; vT.fontSize = 20; vT.fontStyle = FontStyles.Bold;
            vT.alignment = TextAlignmentOptions.Center; vT.color = ACCENT; vT.raycastTarget = false;

            var lGo = Go("L", box.transform);
            var lRt = lGo.AddComponent<RectTransform>();
            lRt.anchorMin = lRt.anchorMax = new Vector2(0.5f, 0f);
            lRt.pivot = new Vector2(0.5f, 0f);
            lRt.sizeDelta = new Vector2(w - 10, 16);
            lRt.anchoredPosition = new Vector2(0, 3);
            var lT = lGo.AddComponent<TextMeshProUGUI>();
            lT.text = label; lT.fontSize = 10; lT.color = TXT_DIM;
            lT.alignment = TextAlignmentOptions.Center; lT.raycastTarget = false;
        }

        // ════════════════════════════════
        //  Saves Panel
        // ════════════════════════════════
        void ShowSavesPanel()
        {
            if (_savesPanel != null) Destroy(_savesPanel);
            _savesPanel = Go("SavesPanel", _root);
            var oRt = _savesPanel.AddComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one; oRt.sizeDelta = Vector2.zero;
            _savesPanel.AddComponent<Image>().color = OVERLAY;
            _savesPanel.AddComponent<Button>().onClick.AddListener(() => { });

            var card = Go("Card", _savesPanel.transform);
            var pcRt = card.AddComponent<RectTransform>();
            pcRt.anchorMin = pcRt.anchorMax = new Vector2(0.5f, 0.5f);
            pcRt.sizeDelta = new Vector2(620, 560);
            card.AddComponent<Image>().color = RIGHT_BG;
            var ol = card.AddComponent<Outline>(); ol.effectColor = BORDER; ol.effectDistance = new Vector2(1,1);

            float y = -25f;
            Lbl(card.transform, "Simulări Salvate", 26, FontStyles.Bold, TXT, y, 34, 540);
            y -= 42;
            Sep(card.transform, 500, y);
            y -= 18;

            var saves = SimSaveManager.GetSaveNames();
            if (saves.Length == 0)
            {
                Lbl(card.transform, "Nu există simulări salvate.", 14, FontStyles.Italic, TXT_DIM, y, 22, 500);
            }
            else
            {
                var scrollGo = Go("Scroll", card.transform);
                var sRt = scrollGo.AddComponent<RectTransform>();
                sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 1f);
                sRt.pivot = new Vector2(0.5f, 1f);
                float sH = 370f;
                sRt.sizeDelta = new Vector2(560, sH);
                sRt.anchoredPosition = new Vector2(0, y);
                scrollGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                scrollGo.AddComponent<Mask>().showMaskGraphic = false;
                var sr = scrollGo.AddComponent<ScrollRect>();
                sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;

                var content = Go("Content", scrollGo.transform);
                var cRt = content.AddComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
                cRt.pivot = new Vector2(0.5f, 1f);
                float iH = 78f;
                cRt.sizeDelta = new Vector2(0, saves.Length * iH);
                sr.content = cRt;

                for (int i = 0; i < saves.Length; i++)
                {
                    string sName = saves[i];
                    var info = SimSaveManager.PeekSave(sName);
                    float iy = -(i * iH);

                    var row = Go("Row", content.transform);
                    var rRt = row.AddComponent<RectTransform>();
                    rRt.anchorMin = new Vector2(0, 1); rRt.anchorMax = new Vector2(1, 1);
                    rRt.pivot = new Vector2(0.5f, 1f); rRt.sizeDelta = new Vector2(0, iH);
                    rRt.anchoredPosition = new Vector2(0, iy);
                    row.AddComponent<Image>().color = (i % 2 == 0) ? new Color(0.05f, 0.07f, 0.11f, 0.5f) : new Color(0.07f, 0.09f, 0.14f, 0.3f);

                    TxtAt(row.transform, sName, 15, FontStyles.Bold, TXT, new Vector2(15, 12), new Vector2(280, 22));
                    if (info != null)
                    {
                        string w = !string.IsNullOrEmpty(info.weatherType) ? $" · {info.temperature:F0}°C {info.weatherType}" : "";
                        TxtAt(row.transform, $"Ziua {info.dayNumber} · {info.parcels.Count} parcele{w} · {info.savedAt}",
                            11, FontStyles.Normal, TXT_DIM, new Vector2(15, -10), new Vector2(340, 18));
                    }

                    RowBtn(row.transform, "Încarcă", ACCENT, ACCENT_LT, Color.black, new Vector2(-105, 0), new Vector2(86, 30), 12, () =>
                    { SimSaveManager.LastSaveName = sName; SimLoader.ShouldLoadSave = true; SceneManager.LoadScene(simulationScene); });

                    RowBtn(row.transform, "Șterge", DEL_BG, DEL_H, TXT, new Vector2(-14, 0), new Vector2(78, 30), 12, () =>
                    { SimSaveManager.DeleteSave(sName); Rebuild(); });
                }
            }

            Btn(card.transform, "Închide", BTN_SEC, BTN_SEC_H, TXT_MID, -520f, 34, 12, 300, Rebuild);
        }

        // ════════════════════════════════
        //  Core helpers
        // ════════════════════════════════
        void Rebuild()
        {
            if (_savesPanel != null) { Destroy(_savesPanel); _savesPanel = null; }
            if (_rootCanvas != null) Destroy(_rootCanvas);
            Build();
        }

        string GetNextRunName()
        {
            var saves = SimSaveManager.GetSaveNames();
            int max = 0;
            foreach (var s in saves)
                if (s.StartsWith("Run_") && s.Length == 7 && int.TryParse(s.Substring(4), out int n) && n > max) max = n;
            return $"Run_{(max + 1):D3}";
        }

        static GameObject Go(string name, Transform parent)
        { var g = new GameObject(name); g.transform.SetParent(parent, false); return g; }

        void StretchImg(Transform p, string n, Color c)
        {
            var g = Go(n, p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            g.AddComponent<Image>().color = c;
        }

        TextMeshProUGUI Lbl(Transform p, string text, float sz, FontStyles fs, Color c, float yPos, float h, float w)
        {
            var g = Go("L", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0, yPos);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = sz; t.fontStyle = fs;
            t.alignment = TextAlignmentOptions.Center; t.color = c;
            t.raycastTarget = false; t.enableWordWrapping = true;
            return t;
        }

        void TxtAt(Transform p, string text, float sz, FontStyles fs, Color c, Vector2 pos, Vector2 size)
        {
            var g = Go("T", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = sz; t.fontStyle = fs; t.color = c; t.raycastTarget = false;
        }

        void Sep(Transform p, float w, float yPos)
        {
            var g = Go("Sep", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, 1);
            rt.anchoredPosition = new Vector2(0, yPos);
            g.AddComponent<Image>().color = SEP;
        }

        void Btn(Transform p, string text, Color bg, Color hov, Color tc, float yPos, float h, float fSz, float w, UnityEngine.Events.UnityAction act)
        {
            var g = Go("Btn", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0, yPos);
            var img = g.AddComponent<Image>(); img.color = bg;
            var btn = g.AddComponent<Button>(); btn.targetGraphic = img;
            var cb = btn.colors; cb.normalColor = bg; cb.highlightedColor = hov;
            cb.pressedColor = bg * 0.75f; cb.selectedColor = bg; cb.fadeDuration = 0.1f;
            btn.colors = cb;
            if (act != null) btn.onClick.AddListener(act);
            var lbl = Go("T", g.transform);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one; lRt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = fSz; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = tc; t.raycastTarget = false;
        }

        void RowBtn(Transform p, string text, Color bg, Color hov, Color tc, Vector2 pos, Vector2 sz, float fSz, UnityEngine.Events.UnityAction act)
        {
            var g = Go("RBtn", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = sz; rt.anchoredPosition = pos;
            var img = g.AddComponent<Image>(); img.color = bg;
            var btn = g.AddComponent<Button>(); btn.targetGraphic = img;
            var cb = btn.colors; cb.normalColor = bg; cb.highlightedColor = hov;
            cb.pressedColor = bg * 0.75f; cb.selectedColor = bg; cb.fadeDuration = 0.1f;
            btn.colors = cb;
            if (act != null) btn.onClick.AddListener(act);
            var lbl = Go("T", g.transform);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one; lRt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = fSz; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = tc; t.raycastTarget = false;
        }
    }
}
