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

        private enum MenuTab { Simulare, Roboti, Plante, Despre }
        private static MenuTab currentTab = MenuTab.Simulare;
        private static int selectedCropIndex = 0;
        private static int selectedRobotIndex = 0;

        // ═══ Palette ═══
        static readonly Color BG         = new Color(0.018f, 0.020f, 0.042f);
        static readonly Color HEADER_BG  = new Color(0.028f, 0.032f, 0.060f, 0.98f);
        static readonly Color SIDE_BG    = new Color(0.032f, 0.036f, 0.072f, 0.97f);
        static readonly Color CONTENT_BG = new Color(0.042f, 0.048f, 0.088f, 0.93f);
        static readonly Color ACCENT     = new Color(0.16f, 0.72f, 0.64f);
        static readonly Color ACCENT_LT  = new Color(0.22f, 0.82f, 0.72f);
        static readonly Color GOLD       = new Color(0.85f, 0.68f, 0.32f);
        static readonly Color GOLD_DIM   = new Color(0.55f, 0.44f, 0.22f);
        static readonly Color TXT        = new Color(0.92f, 0.93f, 0.96f);
        static readonly Color TXT_DIM    = new Color(0.40f, 0.43f, 0.52f);
        static readonly Color TXT_MID    = new Color(0.62f, 0.66f, 0.74f);
        static readonly Color BTN_SEC    = new Color(0.06f, 0.07f, 0.12f);
        static readonly Color BTN_SEC_H  = new Color(0.10f, 0.12f, 0.18f);
        static readonly Color BTN_Q      = new Color(0.12f, 0.05f, 0.05f);
        static readonly Color BTN_Q_H    = new Color(0.22f, 0.08f, 0.08f);
        static readonly Color SEP        = new Color(1f, 1f, 1f, 0.04f);
        static readonly Color BORDER     = new Color(0.16f, 0.72f, 0.64f, 0.06f);
        static readonly Color OVERLAY    = new Color(0.01f, 0.015f, 0.03f, 0.92f);
        static readonly Color DEL_BG     = new Color(0.50f, 0.16f, 0.16f);
        static readonly Color DEL_H      = new Color(0.65f, 0.20f, 0.20f);
        static readonly Color STAT_BG    = new Color(0.030f, 0.035f, 0.065f, 0.85f);

        // ═══ Layout ═══
        const float HDR_H  = 38f;
        const float FTR_H  = 32f;
        const float SIDE_R = 0.175f;

        Transform _root;
        GameObject _rootCanvas, _savesPanel;
        AudioSource menuAudio;
        Camera _prevCam;
        RenderTexture _prevRT;
        GameObject _prevRoot, _prevObj;

        // ═══════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════
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

            // Ensure there is an active camera rendering to the display to prevent the "No cameras rendering" warning
            var existingCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            bool hasScreenCam = false;
            foreach (var cam in existingCams)
            {
                if (cam.enabled && cam.targetTexture == null)
                {
                    hasScreenCam = true;
                    break;
                }
            }
            if (!hasScreenCam)
            {
                var camGo = new GameObject("MainMenuCamera");
                var mainCam = camGo.AddComponent<Camera>();
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = BG;
                mainCam.cullingMask = 0; // Clear screen only
            }

            // Ensure there is an active AudioListener in the Main Menu scene before playing audio
            AudioListener activeListener = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Exclude);
            if (activeListener == null)
            {
                AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                bool enabledExisting = false;
                foreach (var listener in allListeners)
                {
                    if (!listener.enabled && listener.gameObject.activeInHierarchy)
                    {
                        listener.enabled = true;
                        enabledExisting = true;
                        break;
                    }
                }

                if (!enabledExisting)
                {
                    var mainCam = Camera.main;
                    if (mainCam != null && mainCam.gameObject.activeInHierarchy)
                    {
                        var listener = mainCam.GetComponent<AudioListener>();
                        if (listener == null) mainCam.gameObject.AddComponent<AudioListener>();
                        else listener.enabled = true;
                    }
                    else
                    {
                        var anyCam = FindFirstObjectByType<Camera>();
                        if (anyCam != null && anyCam.gameObject.activeInHierarchy)
                        {
                            var listener = anyCam.GetComponent<AudioListener>();
                            if (listener == null) anyCam.gameObject.AddComponent<AudioListener>();
                            else listener.enabled = true;
                        }
                        else
                        {
                            GameObject audioListenerGo = new GameObject("MainMenuAudioListener");
                            audioListenerGo.AddComponent<AudioListener>();
                        }
                    }
                }
            }

            menuAudio = gameObject.GetComponent<AudioSource>();
            if (menuAudio == null) menuAudio = gameObject.AddComponent<AudioSource>();
            AudioClip clip = Resources.Load<AudioClip>("Audio/menu_music");
            if (clip == null) clip = Resources.Load<AudioClip>("Audio/tractor");
            if (clip != null)
            {
                menuAudio.clip = clip;
                menuAudio.loop = true;
                menuAudio.volume = 0.08f;
                if (!menuAudio.isPlaying) menuAudio.Play();
            }

            InitPreview();
            Build();
        }

        void Update()
        {
            if (_prevObj != null)
                _prevObj.transform.Rotate(Vector3.up, 25f * Time.unscaledDeltaTime);
        }

        void OnDestroy()
        {
            if (_prevRT != null) { _prevRT.Release(); Destroy(_prevRT); }
            if (_prevRoot != null) Destroy(_prevRoot);
        }

        void PlayClickSound()
        {
            if (menuAudio == null) return;
            AudioClip c = Resources.Load<AudioClip>("Audio/click");
            if (c != null) menuAudio.PlayOneShot(c, 0.15f);
        }

        // ═══════════════════════════════
        //  3D Preview System
        // ═══════════════════════════════
        void InitPreview()
        {
            _prevRT = new RenderTexture(800, 400, 24);
            _prevRT.antiAliasing = 2;
            _prevRT.Create();

            _prevRoot = new GameObject("__Preview__");
            _prevRoot.transform.position = new Vector3(0, -500, 0);

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(_prevRoot.transform);
            camGo.transform.localPosition = new Vector3(0, 1f, -5f);
            camGo.transform.localRotation = Quaternion.Euler(10, 0, 0);
            _prevCam = camGo.AddComponent<Camera>();
            _prevCam.targetTexture = _prevRT;
            _prevCam.clearFlags = CameraClearFlags.SolidColor;
            _prevCam.backgroundColor = new Color(0.025f, 0.030f, 0.055f, 1f);
            _prevCam.fieldOfView = 45;
            _prevCam.nearClipPlane = 0.01f;
            _prevCam.farClipPlane = 200f;
            _prevCam.enabled = false;

            var keyGo = new GameObject("KeyLight");
            keyGo.transform.SetParent(_prevRoot.transform);
            keyGo.transform.localPosition = new Vector3(3f, 5f, -4f);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Point; key.intensity = 25f; key.range = 25f;
            key.color = new Color(1f, 0.97f, 0.92f);

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(_prevRoot.transform);
            fillGo.transform.localPosition = new Vector3(-4f, 3f, -3f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point; fill.intensity = 12f; fill.range = 25f;
            fill.color = new Color(0.7f, 0.85f, 1f);
        }

        void ClearPreview()
        {
            if (_prevObj != null) { Destroy(_prevObj); _prevObj = null; }
            if (_prevCam != null) _prevCam.enabled = false;
        }

        void PreviewCrop(int idx)
        {
            ClearPreview();
            CropDatabase db = CropLoader.Load();
            if (db?.crops == null || idx < 0 || idx >= db.crops.Length) return;
            string fn = System.IO.Path.GetFileNameWithoutExtension(db.crops[idx].prefabPath);
            var prefab = Resources.Load<GameObject>("CropPrefabs/" + fn);
            if (prefab == null) return;

            _prevObj = new GameObject("PreviewWrapper");
            _prevObj.transform.SetParent(_prevRoot.transform, false);
            _prevObj.transform.localPosition = Vector3.zero;

            var child = Instantiate(prefab, _prevObj.transform);
            CleanObj(child);
            FrameObj(child, 0.8f);
            if (_prevCam != null) _prevCam.enabled = true;
        }

        void PreviewRobot(int idx)
        {
            ClearPreview();
            RobotDatabase db = RobotDataLoader.Load();
            if (db?.robots == null || idx < 0 || idx >= db.robots.Count) return;
            string prefix = db.robots[idx].namePrefix;
            var prefab = Resources.Load<GameObject>("RobotPrefabs/" + prefix);
            if (prefab == null) prefab = Resources.Load<GameObject>("RobotPrefabs/" + prefix + "_Hybrid");
            if (prefab == null)
            {
                var all = Resources.LoadAll<GameObject>("RobotPrefabs");
                foreach (var p in all)
                    if (p.name.Contains(prefix) || prefix.Contains(p.name)) { prefab = p; break; }
            }
            if (prefab == null) return;

            _prevObj = new GameObject("PreviewWrapper");
            _prevObj.transform.SetParent(_prevRoot.transform, false);
            _prevObj.transform.localPosition = Vector3.zero;

            var child = Instantiate(prefab, _prevObj.transform);
            CleanObj(child);
            FrameObj(child, 3f);
            if (_prevCam != null) _prevCam.enabled = true;
        }

        void CleanObj(GameObject obj)
        {
            foreach (var mb in obj.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            foreach (var rb in obj.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
            foreach (var col in obj.GetComponentsInChildren<Collider>()) col.enabled = false;
            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>()) ps.Stop();
        }

        Bounds CalculateLocalBounds(GameObject target)
        {
            Bounds localBounds = new Bounds();
            bool hasBounds = false;

            var filters = target.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null) continue;

                Bounds meshBounds = filter.sharedMesh.bounds;
                Matrix4x4 localMatrix = target.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                Vector3[] corners = new Vector3[]
                {
                    min,
                    max,
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z)
                };

                foreach (var corner in corners)
                {
                    Vector3 transformedCorner = localMatrix.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(transformedCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(transformedCorner);
                    }
                }
            }

            var skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinnedRenderers)
            {
                if (smr.sharedMesh == null) continue;

                Bounds meshBounds = smr.sharedMesh.bounds;
                Matrix4x4 localMatrix = target.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                Vector3[] corners = new Vector3[]
                {
                    min,
                    max,
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z)
                };

                foreach (var corner in corners)
                {
                    Vector3 transformedCorner = localMatrix.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(transformedCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(transformedCorner);
                    }
                }
            }

            if (!hasBounds) return new Bounds(Vector3.zero, Vector3.one);
            return localBounds;
        }

        void FrameObj(GameObject child, float targetSize)
        {
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;

            Bounds localBounds = CalculateLocalBounds(child);
            float maxExtent = Mathf.Max(localBounds.size.x, Mathf.Max(localBounds.size.y, localBounds.size.z));
            if (maxExtent < 0.0001f) maxExtent = 1f;

            float desiredSize = targetSize;
            float scaleFactor = desiredSize / maxExtent;
            child.transform.localScale = Vector3.one * scaleFactor;

            // Center the model in the wrapper
            child.transform.localPosition = -localBounds.center * scaleFactor;

            // Position camera to frame the object nicely
            float dist = desiredSize * 2.5f;
            _prevCam.transform.localPosition = new Vector3(0, desiredSize * 0.35f, -dist);
            _prevCam.transform.LookAt(_prevObj.transform.position);
        }


        // ═══════════════════════════════
        //  Build
        // ═══════════════════════════════
        void Build()
        {
            var root = new GameObject("MenuCanvas");
            var cv = root.AddComponent<UnityEngine.Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = root.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();
            _rootCanvas = root; _root = root.transform;

            StretchImg(_root, "BG", BG);

            // ── Header ──
            var hdr = Go("Header", _root);
            var hRt = Stretch(hdr, 0, 1, 1, 1, 0, 0, 0, HDR_H);
            hdr.AddComponent<Image>().color = HEADER_BG;

            // Header accent line at bottom
            var hLine = Go("HdrLine", hdr.transform);
            var hlRt = Stretch(hLine, 0, 0, 1, 0, 0, 0, 0, 1.5f);
            hLine.AddComponent<Image>().color = new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.35f);

            // Header logo
            var logoTex = Resources.Load<Texture2D>("ASE_Logo");
            if (logoTex != null)
            {
                var hLogo = Go("HLogo", hdr.transform);
                var hlgRt = hLogo.AddComponent<RectTransform>();
                hlgRt.anchorMin = hlgRt.anchorMax = new Vector2(0, 0.5f);
                hlgRt.pivot = new Vector2(0, 0.5f);
                hlgRt.sizeDelta = new Vector2(26, 26);
                hlgRt.anchoredPosition = new Vector2(14, 0);
                var hlImg = hLogo.AddComponent<Image>();
                hlImg.sprite = Sprite.Create(logoTex, new Rect(0, 0, logoTex.width, logoTex.height), Vector2.one * 0.5f);
                hlImg.preserveAspect = true; hlImg.raycastTarget = false;
            }

            TxtAt(hdr.transform, "<b>AGROBOT</b>  Simulator Agricol Multi-Agent", 12, FontStyles.Normal, TXT_MID,
                new Vector2(48, 0), new Vector2(400, 20));

            var hRight = Go("HRight", hdr.transform);
            var hrRt = hRight.AddComponent<RectTransform>();
            hrRt.anchorMin = hrRt.anchorMax = new Vector2(1, 0.5f);
            hrRt.pivot = new Vector2(1, 0.5f);
            hrRt.sizeDelta = new Vector2(500, 20);
            hrRt.anchoredPosition = new Vector2(-14, 0);
            var hrT = hRight.AddComponent<TextMeshProUGUI>();
            hrT.text = "Academia de Studii Economice din București  ·  CSIE  ·  2026";
            hrT.fontSize = 11; hrT.color = TXT_DIM;
            hrT.alignment = TextAlignmentOptions.Right; hrT.raycastTarget = false;

            // ── Footer ──
            var ftr = Go("Footer", _root);
            Stretch(ftr, 0, 0, 1, 0, 0, FTR_H, 0, 0);
            ftr.AddComponent<Image>().color = HEADER_BG;

            Lbl(ftr.transform, "Mircea Ștefăniță-Leonard  ·  Lucrare de licență  ·  ASE București 2026",
                11, FontStyles.Normal, TXT_DIM, 0, FTR_H, 800);

            var vGo = Go("Ver", ftr.transform);
            var vRt = vGo.AddComponent<RectTransform>();
            vRt.anchorMin = vRt.anchorMax = new Vector2(1, 0.5f);
            vRt.pivot = new Vector2(1, 0.5f);
            vRt.sizeDelta = new Vector2(140, 16);
            vRt.anchoredPosition = new Vector2(-14, 0);
            var vt = vGo.AddComponent<TextMeshProUGUI>();
            vt.text = "v1.0 · Unity 6"; vt.fontSize = 10;
            vt.alignment = TextAlignmentOptions.Right; vt.color = TXT_DIM; vt.raycastTarget = false;

            // ── Left Sidebar ──
            var side = Go("Sidebar", _root);
            var sRt = side.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0); sRt.anchorMax = new Vector2(SIDE_R, 1);
            sRt.offsetMin = new Vector2(0, FTR_H); sRt.offsetMax = new Vector2(0, -HDR_H);
            side.AddComponent<Image>().color = SIDE_BG;

            // Sidebar left accent stripe
            var stripe = Go("Stripe", side.transform);
            var strRt = stripe.AddComponent<RectTransform>();
            strRt.anchorMin = new Vector2(0, 0.05f); strRt.anchorMax = new Vector2(0, 0.95f);
            strRt.pivot = new Vector2(0, 0.5f); strRt.sizeDelta = new Vector2(3, 0);
            stripe.AddComponent<Image>().color = ACCENT;

            // Sidebar content
            float sy = -25f;

            if (logoTex != null)
            {
                var logo = Go("Logo", side.transform);
                var logoRt = logo.AddComponent<RectTransform>();
                logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 1f);
                logoRt.pivot = new Vector2(0.5f, 1f);
                logoRt.sizeDelta = new Vector2(88, 68);
                logoRt.anchoredPosition = new Vector2(0, sy);
                var lImg = logo.AddComponent<Image>();
                lImg.sprite = Sprite.Create(logoTex, new Rect(0, 0, logoTex.width, logoTex.height), Vector2.one * 0.5f);
                lImg.preserveAspect = true; lImg.raycastTarget = false;
                sy -= 78;
            }

            var ttl = Lbl(side.transform, "AgroBot", 36, FontStyles.Bold, TXT, sy, 44, 300);
            ttl.characterSpacing = 5; sy -= 40;
            Lbl(side.transform, "Simulator Agricol\nMulti-Agent", 12, FontStyles.Italic, ACCENT, sy, 32, 260);
            sy -= 38;
            Lbl(side.transform, "Lucrare de licență\nCSIE - 2026", 10, FontStyles.Normal, GOLD_DIM, sy, 28, 240);
            sy -= 40;

            // Tab buttons
            string[] tabNames = { "Simulare", "Roboți", "Plante", "Despre aplicație" };
            MenuTab[] tabs = { MenuTab.Simulare, MenuTab.Roboti, MenuTab.Plante, MenuTab.Despre };
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                TabBtn(side.transform, tabNames[idx], currentTab == tabs[idx], sy, () => {
                    currentTab = tabs[idx]; Rebuild();
                });
                sy -= 40;
            }

            // Bottom credits — anchored to bottom
            var btm = Go("Credits", side.transform);
            var btmRt = btm.AddComponent<RectTransform>();
            btmRt.anchorMin = new Vector2(0, 0); btmRt.anchorMax = new Vector2(1, 0);
            btmRt.pivot = new Vector2(0.5f, 0f);
            btmRt.sizeDelta = new Vector2(0, 85); btmRt.anchoredPosition = new Vector2(0, 12);

            Lbl(btm.transform, "Coordonator", 10, FontStyles.Bold, GOLD_DIM, -5f, 14, 260);
            Lbl(btm.transform, "Lect. dr. Zurini Mădălina", 11, FontStyles.Normal, TXT_MID, -20f, 16, 260);
            Lbl(btm.transform, "Absolvent", 10, FontStyles.Bold, GOLD_DIM, -42f, 14, 260);
            Lbl(btm.transform, "Mircea Ștefăniță-Leonard", 11, FontStyles.Normal, TXT_MID, -57f, 16, 260);

            // ── Vertical Divider ──
            var div = Go("Div", _root);
            var divRt = div.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(SIDE_R, 0); divRt.anchorMax = new Vector2(SIDE_R, 1);
            divRt.offsetMin = new Vector2(0, FTR_H + 30); divRt.offsetMax = new Vector2(0, -HDR_H - 30);
            divRt.sizeDelta = new Vector2(1, divRt.sizeDelta.y);
            div.AddComponent<Image>().color = new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.20f);

            // ── Right Content ──
            var content = Go("Content", _root);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(SIDE_R, 0); cRt.anchorMax = new Vector2(1, 1);
            cRt.offsetMin = new Vector2(0, FTR_H); cRt.offsetMax = new Vector2(0, -HDR_H);
            content.AddComponent<Image>().color = CONTENT_BG;

            switch (currentTab)
            {
                case MenuTab.Simulare: BuildSimulare(content.transform); break;
                case MenuTab.Plante:   BuildPlante(content.transform);   break;
                case MenuTab.Roboti:   BuildRoboti(content.transform);   break;
                case MenuTab.Despre:   BuildDespre(content.transform);   break;
            }
        }

        // ═══════════════════════════════
        //  Tab: Simulare
        // ═══════════════════════════════
        void BuildSimulare(Transform p)
        {
            string nextRun = GetNextRunName();
            var saves = SimSaveManager.GetSaveNames();

            float y = -35f;
            LblL(p, "Simulare", 28, FontStyles.Bold, TXT, 30, y, 36, 500); y -= 45;

            LblL(p, "Sesiunea următoare va fi salvată ca <b>" + nextRun +
                "</b>. Pornește o simulare nouă pentru a observa comportamentul agenților autonomi în mediul agricol virtual.",
                13, FontStyles.Normal, TXT_MID, 30, y, 40, 1200).richText = true;
            y -= 55;

            WBtn(p, $"Pornește simularea  ·  {nextRun}", ACCENT, ACCENT_LT, new Color(0.02f, 0.04f, 0.03f),
                y, 52, 15, 30, () => {
                    SimSaveManager.LastSaveName = nextRun;
                    SimLoader.ShouldLoadSave = false;
                    SceneManager.LoadScene(simulationScene);
                }); y -= 70;

            LblL(p, "Sesiuni anterioare", 12, FontStyles.Bold, GOLD, 30, y, 18, 300); y -= 28;

            string savesLbl = $"Deschide lista de simulări  ({saves.Length})";
            WBtn(p, savesLbl, BTN_SEC, BTN_SEC_H, ACCENT, y, 46, 13, 30, ShowSavesPanel); y -= 65;

            LblL(p, "Sistem", 12, FontStyles.Bold, TXT_MID, 30, y, 18, 300); y -= 28;

            WBtn(p, "Ieșire din aplicație", BTN_Q, BTN_Q_H, TXT_DIM, y, 42, 13, 30, () => {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // Status bar at bottom
            var st = Go("Status", p);
            var stRt = st.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 0); stRt.anchorMax = new Vector2(0.5f, 0);
            stRt.pivot = new Vector2(0.5f, 0); stRt.sizeDelta = new Vector2(900, 16);
            stRt.anchoredPosition = new Vector2(0, 14);
            var stT = st.AddComponent<TextMeshProUGUI>();
            stT.text = "<color=#29B8A3>●</color>  ML Activ     <color=#29B8A3>●</color>  Bază de date conectată     <color=#29B8A3>●</color>  Senzori calibrați";
            stT.fontSize = 10; stT.richText = true; stT.color = TXT_DIM;
            stT.alignment = TextAlignmentOptions.Center; stT.raycastTarget = false;
        }

        // ═══════════════════════════════
        //  Tab: Plante
        // ═══════════════════════════════
        void BuildPlante(Transform p)
        {
            LblL(p, "Bază de Date Culturi", 22, FontStyles.Bold, TXT, 25, -20, 30, 500);

            CropDatabase db = CropLoader.Load();
            if (db?.crops == null) return;

            // Crop list — left column
            var listArea = Go("ListArea", p);
            var laRt = listArea.AddComponent<RectTransform>();
            laRt.anchorMin = new Vector2(0, 0); laRt.anchorMax = new Vector2(0.34f, 1);
            laRt.offsetMin = new Vector2(10, 8); laRt.offsetMax = new Vector2(-5, -55);
            listArea.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            listArea.AddComponent<Mask>().showMaskGraphic = false;
            var sr = listArea.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;

            var content = Go("Content", listArea.transform);
            var coRt = content.AddComponent<RectTransform>();
            coRt.anchorMin = new Vector2(0, 1); coRt.anchorMax = new Vector2(1, 1);
            coRt.pivot = new Vector2(0.5f, 1f);
            float rH = 58f;
            coRt.sizeDelta = new Vector2(0, db.crops.Length * rH);
            sr.content = coRt;

            for (int i = 0; i < db.crops.Length; i++)
            {
                int idx = i;
                var c = db.crops[i];
                float iy = -(i * rH);
                bool sel = selectedCropIndex == idx;

                var row = Go("Row_" + idx, content.transform);
                var rowRt = row.AddComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
                rowRt.pivot = new Vector2(0.5f, 1f); rowRt.sizeDelta = new Vector2(0, rH - 4);
                rowRt.anchoredPosition = new Vector2(0, iy);
                row.AddComponent<Image>().color = sel ? new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.14f)
                                                      : new Color(0.06f, 0.08f, 0.13f, 0.35f);
                var btn = row.AddComponent<Button>();
                btn.onClick.AddListener(() => { PlayClickSound(); selectedCropIndex = idx; Rebuild(); });

                TxtAt(row.transform, c.name, 14, FontStyles.Bold, sel ? ACCENT : TXT, new Vector2(12, 10), new Vector2(180, 18));
                TxtAt(row.transform, "Cultură Agricolă", 9, FontStyles.Italic, GOLD_DIM, new Vector2(12, -10), new Vector2(140, 14));
                TxtAt(row.transform, $"Ciclu: {c.growthDays} zile · Preț: {c.marketPricePerKg:F2} EUR/kg",
                    9, FontStyles.Normal, TXT_DIM, new Vector2(12, -24), new Vector2(280, 14));
            }

            // Details card — right column
            if (selectedCropIndex >= 0 && selectedCropIndex < db.crops.Length)
            {
                var crop = db.crops[selectedCropIndex];
                var det = Go("Details", p);
                var dRt = det.AddComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0.36f, 0); dRt.anchorMax = new Vector2(1, 1);
                dRt.offsetMin = new Vector2(5, 8); dRt.offsetMax = new Vector2(-12, -55);
                det.AddComponent<Image>().color = new Color(0.035f, 0.040f, 0.078f, 0.65f);
                var dOl = det.AddComponent<Outline>();
                dOl.effectColor = BORDER; dOl.effectDistance = new Vector2(1, 1);

                float dy = -15f;
                LblL(det.transform, "Detalii Cultură", 16, FontStyles.Bold, GOLD, 18, dy, 22, 300);
                dy -= 28;

                // Preview box
                var vis = Go("VisBox", det.transform);
                var visRt = vis.AddComponent<RectTransform>();
                visRt.anchorMin = new Vector2(0.03f, 1); visRt.anchorMax = new Vector2(0.97f, 1);
                visRt.pivot = new Vector2(0.5f, 1); visRt.sizeDelta = new Vector2(0, 180);
                visRt.anchoredPosition = new Vector2(0, dy);
                vis.AddComponent<Image>().color = STAT_BG;
                var visOl = vis.AddComponent<Outline>();
                visOl.effectColor = BORDER; visOl.effectDistance = new Vector2(1, 1);

                LblL(vis.transform, "VIZUALIZARE 3D", 8, FontStyles.Bold, TXT_DIM, 8, -4, 12, 120);

                // RawImage for 3D preview
                var rawGo = Go("Preview", vis.transform);
                var rawRt = rawGo.AddComponent<RectTransform>();
                rawRt.anchorMin = new Vector2(0.02f, 0.02f); rawRt.anchorMax = new Vector2(0.98f, 0.88f);
                rawRt.offsetMin = Vector2.zero; rawRt.offsetMax = Vector2.zero;
                var rawImg = rawGo.AddComponent<RawImage>();
                rawImg.texture = _prevRT;

                PreviewCrop(selectedCropIndex);
                dy -= 195;

                // 3-column stats grid
                float gx1 = 18f, gx2 = 18f + 310f, gx3 = 18f + 620f;
                float gw = 295f, gh = 42f;

                GridStat(det.transform, gx1, dy, "PREȚ PIAȚĂ", $"{crop.marketPricePerKg:F2} EUR/kg", gw, gh);
                GridStat(det.transform, gx2, dy, "CICLU CREȘTERE", $"{crop.growthDays} zile", gw, gh);
                GridStat(det.transform, gx3, dy, "COST SĂMÂNȚĂ", $"{crop.seedCostEUR:F3} EUR", gw, gh);
                dy -= gh + 8;

                GridStat(det.transform, gx1, dy, "TEMP. OPTIMĂ", $"{crop.tempOptimal:F0}°C", gw, gh);
                GridStat(det.transform, gx2, dy, "REZ. ÎNGHEȚ", crop.isFrostResistant ? "Rezistentă" : "Sensibilă", gw, gh);
                GridStat(det.transform, gx3, dy, "PH OPTIM SOL", $"{crop.requirements.pH.optimal:F1} pH", gw, gh);
                dy -= gh + 18;

                // Cultivation parameters
                LblL(det.transform, "Parametri de Cultivare", 11, FontStyles.Bold, ACCENT, 18, dy, 16, 300);
                dy -= 20;

                LblL(det.transform, $"Umiditate Optimă Sol:  {crop.requirements.humidity.optimal}% (interval {crop.requirements.humidity.min}% - {crop.requirements.humidity.max}%)",
                    9.5f, FontStyles.Normal, TXT_MID, 18, dy, 15, 900); dy -= 16;

                LblL(det.transform, $"Interval Temperaturi Tolerabil:  {crop.tempMin}°C - {crop.tempMax}°C",
                    9.5f, FontStyles.Normal, TXT_MID, 18, dy, 15, 900); dy -= 16;

                LblL(det.transform, $"Cerințe Nutrienți NPK Sol:  N: {crop.requirements.nitrogen.optimal}  ·  P: {crop.requirements.phosphorus.optimal}  ·  K: {crop.requirements.potassium.optimal} kg/ha",
                    9.5f, FontStyles.Normal, TXT_MID, 18, dy, 15, 900);

                // Source — anchored to bottom
                if (!string.IsNullOrEmpty(crop.source))
                {
                    var src = Go("Src", det.transform);
                    var srcRt = src.AddComponent<RectTransform>();
                    srcRt.anchorMin = srcRt.anchorMax = new Vector2(0, 0);
                    srcRt.pivot = new Vector2(0, 0); srcRt.sizeDelta = new Vector2(900, 26);
                    srcRt.anchoredPosition = new Vector2(18, 8);
                    var sT = src.AddComponent<TextMeshProUGUI>();
                    sT.text = "Referință Științifică:  " + crop.source;
                    sT.fontSize = 8; sT.fontStyle = FontStyles.Italic;
                    sT.color = TXT_DIM; sT.raycastTarget = false; sT.enableWordWrapping = true;
                }
            }
        }

        // ═══════════════════════════════
        //  Tab: Roboți
        // ═══════════════════════════════
        void BuildRoboti(Transform p)
        {
            LblL(p, "Bază de Date Roboți", 22, FontStyles.Bold, TXT, 25, -20, 30, 500);

            RobotDatabase db = RobotDataLoader.Load();
            if (db?.robots == null) return;

            // Robot list — left column
            var listArea = Go("ListArea", p);
            var laRt = listArea.AddComponent<RectTransform>();
            laRt.anchorMin = new Vector2(0, 0); laRt.anchorMax = new Vector2(0.34f, 1);
            laRt.offsetMin = new Vector2(10, 8); laRt.offsetMax = new Vector2(-5, -55);
            listArea.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            listArea.AddComponent<Mask>().showMaskGraphic = false;
            var sr = listArea.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;

            var content = Go("Content", listArea.transform);
            var coRt = content.AddComponent<RectTransform>();
            coRt.anchorMin = new Vector2(0, 1); coRt.anchorMax = new Vector2(1, 1);
            coRt.pivot = new Vector2(0.5f, 1f);
            float rH = 58f;
            coRt.sizeDelta = new Vector2(0, db.robots.Count * rH);
            sr.content = coRt;

            for (int i = 0; i < db.robots.Count; i++)
            {
                int idx = i;
                var r = db.robots[i];
                float iy = -(i * rH);
                bool sel = selectedRobotIndex == idx;

                var row = Go("Row_" + idx, content.transform);
                var rowRt = row.AddComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
                rowRt.pivot = new Vector2(0.5f, 1f); rowRt.sizeDelta = new Vector2(0, rH - 4);
                rowRt.anchoredPosition = new Vector2(0, iy);
                row.AddComponent<Image>().color = sel ? new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.14f)
                                                      : new Color(0.06f, 0.08f, 0.13f, 0.35f);
                var btn = row.AddComponent<Button>();
                btn.onClick.AddListener(() => { PlayClickSound(); selectedRobotIndex = idx; Rebuild(); });

                TxtAt(row.transform, r.namePrefix, 14, FontStyles.Bold, sel ? ACCENT : TXT, new Vector2(12, 10), new Vector2(180, 18));
                TxtAt(row.transform, "Echipament Autonom", 9, FontStyles.Italic, GOLD_DIM, new Vector2(12, -10), new Vector2(140, 14));
                TxtAt(row.transform, r.model, 9, FontStyles.Normal, TXT_DIM, new Vector2(12, -24), new Vector2(280, 14));
            }

            // Details card — right column
            if (selectedRobotIndex >= 0 && selectedRobotIndex < db.robots.Count)
            {
                var robot = db.robots[selectedRobotIndex];
                var det = Go("Details", p);
                var dRt = det.AddComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0.36f, 0); dRt.anchorMax = new Vector2(1, 1);
                dRt.offsetMin = new Vector2(5, 8); dRt.offsetMax = new Vector2(-12, -55);
                det.AddComponent<Image>().color = new Color(0.035f, 0.040f, 0.078f, 0.65f);
                var dOl = det.AddComponent<Outline>();
                dOl.effectColor = BORDER; dOl.effectDistance = new Vector2(1, 1);

                float dy = -15f;
                LblL(det.transform, "Detalii Robot", 16, FontStyles.Bold, GOLD, 18, dy, 22, 300);
                dy -= 28;

                // Preview box
                var vis = Go("VisBox", det.transform);
                var visRt = vis.AddComponent<RectTransform>();
                visRt.anchorMin = new Vector2(0.03f, 1); visRt.anchorMax = new Vector2(0.97f, 1);
                visRt.pivot = new Vector2(0.5f, 1); visRt.sizeDelta = new Vector2(0, 180);
                visRt.anchoredPosition = new Vector2(0, dy);
                vis.AddComponent<Image>().color = STAT_BG;
                var visOl = vis.AddComponent<Outline>();
                visOl.effectColor = BORDER; visOl.effectDistance = new Vector2(1, 1);

                LblL(vis.transform, "VIZUALIZARE 3D", 8, FontStyles.Bold, TXT_DIM, 8, -4, 12, 120);

                var rawGo = Go("Preview", vis.transform);
                var rawRt = rawGo.AddComponent<RectTransform>();
                rawRt.anchorMin = new Vector2(0.02f, 0.02f); rawRt.anchorMax = new Vector2(0.98f, 0.88f);
                rawRt.offsetMin = Vector2.zero; rawRt.offsetMax = Vector2.zero;
                var rawImg = rawGo.AddComponent<RawImage>();
                rawImg.texture = _prevRT;

                PreviewRobot(selectedRobotIndex);
                dy -= 195;

                // 3-column stats grid
                float gx1 = 18f, gx2 = 18f + 310f, gx3 = 18f + 620f;
                float gw = 295f, gh = 42f;

                GridStat(det.transform, gx1, dy, "MODEL ECHIPAMENT", robot.model, gw, gh);
                GridStat(det.transform, gx2, dy, "PREȚ ACHIZIȚIE", $"{robot.purchasePrice:N0} EUR", gw, gh);
                GridStat(det.transform, gx3, dy, "CAPACITATE BATERIE", $"{robot.batteryCapacity / 1000f:F1} kWh", gw, gh);
                dy -= gh + 8;

                GridStat(det.transform, gx1, dy, "VITEZĂ MAXIMĂ", $"{robot.maxSpeed} m/s", gw, gh);
                GridStat(det.transform, gx2, dy, "GREUTATE TOTALĂ", $"{robot.weightKg:N0} kg", gw, gh);
                GridStat(det.transform, gx3, dy, "DURATĂ VIAȚĂ UTILĂ", $"{robot.utilityLifeYears} ani", gw, gh);
                dy -= gh + 18;

                // Specifications
                LblL(det.transform, "Specificații Consum & Încărcare", 11, FontStyles.Bold, ACCENT, 18, dy, 16, 400);
                dy -= 20;

                LblL(det.transform, $"Consum deplasare:  {robot.consumptionMeter * 1000f:F1} Wh/m",
                    9.5f, FontStyles.Normal, TXT_MID, 18, dy, 15, 900); dy -= 16;

                LblL(det.transform, $"Consum operare:  {robot.consumptionWorkSec * 1000f:F1} Wh/s (Standby: {robot.consumptionStandbySec * 1000f:F2} Wh/s)",
                    9.5f, FontStyles.Normal, TXT_MID, 18, dy, 15, 900); dy -= 16;

                LblL(det.transform, $"Rată de reîncărcare:  {robot.rechargeRate * 1000f:F1} Wh/s",
                    9.5f, FontStyles.Normal, TXT_MID, 18, dy, 15, 900);

                // Source — anchored to bottom
                if (!string.IsNullOrEmpty(robot.source))
                {
                    var src = Go("Src", det.transform);
                    var srcRt = src.AddComponent<RectTransform>();
                    srcRt.anchorMin = srcRt.anchorMax = new Vector2(0, 0);
                    srcRt.pivot = new Vector2(0, 0); srcRt.sizeDelta = new Vector2(900, 26);
                    srcRt.anchoredPosition = new Vector2(18, 8);
                    var sT = src.AddComponent<TextMeshProUGUI>();
                    sT.text = "Sursă Specificații:  " + robot.source;
                    sT.fontSize = 8; sT.fontStyle = FontStyles.Italic;
                    sT.color = TXT_DIM; sT.raycastTarget = false; sT.enableWordWrapping = true;
                }
            }
        }

        // ═══════════════════════════════
        //  Tab: Despre
        // ═══════════════════════════════
        void BuildDespre(Transform p)
        {
            LblL(p, "Despre Aplicație", 22, FontStyles.Bold, TXT, 25, -20, 30, 500);

            var panel = Go("TextPanel", p);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 0); pRt.anchorMax = new Vector2(1, 1);
            pRt.offsetMin = new Vector2(20, 15); pRt.offsetMax = new Vector2(-20, -60);
            panel.AddComponent<Image>().color = new Color(0.035f, 0.040f, 0.078f, 0.6f);
            var pOl = panel.AddComponent<Outline>();
            pOl.effectColor = BORDER; pOl.effectDistance = new Vector2(1, 1);

            string descText =
                "<b>AgroBot</b> este un simulator agricol multi-agent dezvoltat ca lucrare de licență în cadrul Academiei de Studii Economice din București, Facultatea de Cibernetică, Statistică și Informatică Economică.\n\n" +
                "Sistemul simulează comportamentul colaborativ al unor agenți autonomi (drone de diagnosticare și tratament, plus roboți tereștri de plantare și recoltare) pe o fermă digitală 3D. Obiectivul principal este optimizarea randamentului economic (ROI) al culturilor.\n\n" +
                "<b>Tehnologii Cheie utilizate:</b>\n" +
                "•  <b>Unity & C# Job System:</b> Pentru rularea fluidă a simulării fizice și a creșterii celulare a plantelor în mod paralel.\n" +
                "•  <b>Machine Learning (Decision Tree):</b> Algoritm de clasificare antrenat în Python și integrat direct în C# pentru selecția inteligentă a culturii optime bazată pe compoziția solului (pH, umiditate, NPK), sezon și vreme.\n" +
                "•  <b>A* Pathfinding:</b> Algoritm de navigare inteligentă adaptat pentru ocolirea obstacolelor în timp util.\n" +
                "•  <b>Model Economic ROI:</b> Calculul costurilor cu combustibilul/energia, amortizarea echipamentelor, costul semințelor și profitul obținut la recoltare.";

            var tGo = Go("Desc", panel.transform);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(22, 22); tRt.offsetMax = new Vector2(-22, -22);
            var dT = tGo.AddComponent<TextMeshProUGUI>();
            dT.text = descText; dT.fontSize = 12; dT.color = TXT_MID;
            dT.raycastTarget = false; dT.enableWordWrapping = true; dT.richText = true;
        }

        // ═══════════════════════════════
        //  Saves Panel
        // ═══════════════════════════════
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
            pcRt.sizeDelta = new Vector2(650, 580);
            card.AddComponent<Image>().color = CONTENT_BG;
            var ol = card.AddComponent<Outline>(); ol.effectColor = BORDER; ol.effectDistance = new Vector2(1, 1);

            float y = -25f;
            Lbl(card.transform, "Simulări Salvate", 26, FontStyles.Bold, TXT, y, 34, 560); y -= 42;
            HSep(card.transform, 540, y); y -= 18;

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
                sRt.sizeDelta = new Vector2(590, 385);
                sRt.anchoredPosition = new Vector2(0, y);
                scrollGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
                scrollGo.AddComponent<Mask>().showMaskGraphic = false;
                var sr = scrollGo.AddComponent<ScrollRect>();
                sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;

                var cont = Go("Content", scrollGo.transform);
                var cRt = cont.AddComponent<RectTransform>();
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

                    var row = Go("Row", cont.transform);
                    var rRt = row.AddComponent<RectTransform>();
                    rRt.anchorMin = new Vector2(0, 1); rRt.anchorMax = new Vector2(1, 1);
                    rRt.pivot = new Vector2(0.5f, 1f); rRt.sizeDelta = new Vector2(0, iH - 4);
                    rRt.anchoredPosition = new Vector2(0, iy);
                    row.AddComponent<Image>().color = (i % 2 == 0)
                        ? new Color(0.04f, 0.06f, 0.10f, 0.5f) : new Color(0.06f, 0.08f, 0.13f, 0.3f);

                    TxtAt(row.transform, sName, 15, FontStyles.Bold, TXT, new Vector2(15, 12), new Vector2(300, 22));
                    if (info != null)
                    {
                        string w = !string.IsNullOrEmpty(info.weatherType) ? $" · {info.temperature:F0}°C {info.weatherType}" : "";
                        TxtAt(row.transform, $"Ziua {info.dayNumber} · {info.parcels.Count} parcele{w} · {info.savedAt}",
                            11, FontStyles.Normal, TXT_DIM, new Vector2(15, -10), new Vector2(380, 18));
                    }

                    RowBtn(row.transform, "Încarcă", ACCENT, ACCENT_LT, Color.black, new Vector2(-110, 0), new Vector2(90, 30), 12,
                        () => { SimSaveManager.LastSaveName = sName; SimLoader.ShouldLoadSave = true; SceneManager.LoadScene(simulationScene); });

                    RowBtn(row.transform, "Șterge", DEL_BG, DEL_H, TXT, new Vector2(-14, 0), new Vector2(82, 30), 12,
                        () => { SimSaveManager.DeleteSave(sName); Rebuild(); });
                }
            }

            Btn(card.transform, "Închide", BTN_SEC, BTN_SEC_H, TXT_MID, -535f, 36, 12, 320, Rebuild);
        }

        // ═══════════════════════════════
        //  Rebuild & Utilities
        // ═══════════════════════════════
        void Rebuild()
        {
            if (_savesPanel != null) { Destroy(_savesPanel); _savesPanel = null; }
            if (_rootCanvas != null) Destroy(_rootCanvas);
            ClearPreview();
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

        // ═══════════════════════════════
        //  UI Helpers
        // ═══════════════════════════════
        static GameObject Go(string name, Transform parent)
        { var g = new GameObject(name); g.transform.SetParent(parent, false); return g; }

        /// <summary>Stretch rect helper. Returns the RectTransform.</summary>
        RectTransform Stretch(GameObject go, float aMinX, float aMinY, float aMaxX, float aMaxY,
            float oMinX, float oMinY, float oMaxX, float oMaxY)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(aMinX, aMinY); rt.anchorMax = new Vector2(aMaxX, aMaxY);
            rt.offsetMin = new Vector2(oMinX, oMinY); rt.offsetMax = new Vector2(-oMaxX, -oMaxY);
            return rt;
        }

        void StretchImg(Transform p, string n, Color c)
        {
            var g = Go(n, p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            g.AddComponent<Image>().color = c;
        }

        /// <summary>Centered label pinned to top-center of parent.</summary>
        TextMeshProUGUI Lbl(Transform p, string text, float sz, FontStyles fs, Color c, float yPos, float h, float w)
        {
            var g = Go("L", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0, yPos);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = sz; t.fontStyle = fs;
            t.alignment = TextAlignmentOptions.Center; t.color = c;
            t.raycastTarget = false; t.enableWordWrapping = true; t.richText = true;
            return t;
        }

        /// <summary>Left-aligned label pinned to top-left of parent.</summary>
        TextMeshProUGUI LblL(Transform p, string text, float sz, FontStyles fs, Color c, float x, float y, float h, float w)
        {
            var g = Go("L", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1f);
            rt.pivot = new Vector2(0, 1f); rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = sz; t.fontStyle = fs;
            t.alignment = TextAlignmentOptions.Left; t.color = c;
            t.raycastTarget = false; t.enableWordWrapping = true; t.richText = true;
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
            t.text = text; t.fontSize = sz; t.fontStyle = fs; t.color = c;
            t.raycastTarget = false; t.richText = true;
        }

        void HSep(Transform p, float w, float yPos)
        {
            var g = Go("Sep", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, 1);
            rt.anchoredPosition = new Vector2(0, yPos);
            g.AddComponent<Image>().color = SEP;
        }

        /// <summary>Standard centered button.</summary>
        void Btn(Transform p, string text, Color bg, Color hov, Color tc, float yPos, float h, float fSz, float w,
            UnityEngine.Events.UnityAction act)
        {
            var g = Go("Btn", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0, yPos);
            var img = g.AddComponent<Image>(); img.color = bg;
            var btn = g.AddComponent<Button>(); btn.targetGraphic = img;
            var cb = btn.colors; cb.normalColor = bg; cb.highlightedColor = hov;
            cb.pressedColor = bg * 0.75f; cb.selectedColor = bg; cb.fadeDuration = 0.1f;
            btn.colors = cb;
            if (act != null) btn.onClick.AddListener(() => { PlayClickSound(); act(); });
            var lbl = Go("T", g.transform);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one; lRt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = fSz; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = tc; t.raycastTarget = false;
        }

        /// <summary>Wide stretch button with margins.</summary>
        void WBtn(Transform p, string text, Color bg, Color hov, Color tc, float yPos, float h, float fSz, float margin,
            UnityEngine.Events.UnityAction act)
        {
            var g = Go("Btn", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-margin * 2, h);
            rt.anchoredPosition = new Vector2(0, yPos);
            var img = g.AddComponent<Image>(); img.color = bg;
            var btn = g.AddComponent<Button>(); btn.targetGraphic = img;
            var cb = btn.colors; cb.normalColor = bg; cb.highlightedColor = hov;
            cb.pressedColor = bg * 0.75f; cb.selectedColor = bg; cb.fadeDuration = 0.1f;
            btn.colors = cb;
            if (act != null) btn.onClick.AddListener(() => { PlayClickSound(); act(); });
            var lbl = Go("T", g.transform);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one; lRt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = fSz; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = tc; t.raycastTarget = false;
        }

        /// <summary>Sidebar tab button with left accent bar when selected.</summary>
        void TabBtn(Transform p, string text, bool sel, float yPos, UnityEngine.Events.UnityAction act)
        {
            float h = 36f;
            var g = Go("Tab", p);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-16, h);
            rt.anchoredPosition = new Vector2(0, yPos);

            Color bg = sel ? new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.12f) : Color.clear;
            Color hovBg = sel ? new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.22f) : new Color(0.10f, 0.12f, 0.18f, 0.4f);
            var img = g.AddComponent<Image>(); img.color = bg;
            var btn = g.AddComponent<Button>(); btn.targetGraphic = img;
            var cb = btn.colors; cb.normalColor = bg; cb.highlightedColor = hovBg;
            cb.pressedColor = bg; cb.selectedColor = bg; cb.fadeDuration = 0.08f;
            btn.colors = cb;
            if (act != null) btn.onClick.AddListener(() => { PlayClickSound(); act(); });

            if (sel)
            {
                var bar = Go("AccBar", g.transform);
                var brt = bar.AddComponent<RectTransform>();
                brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(0, 1);
                brt.pivot = new Vector2(0, 0.5f); brt.sizeDelta = new Vector2(3, 0);
                bar.AddComponent<Image>().color = ACCENT;
            }

            var lbl = Go("T", g.transform);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(18, 0); lRt.offsetMax = new Vector2(-6, 0);
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = 13;
            t.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.color = sel ? TXT : TXT_DIM; t.raycastTarget = false;
        }

        void RowBtn(Transform p, string text, Color bg, Color hov, Color tc, Vector2 pos, Vector2 sz, float fSz,
            UnityEngine.Events.UnityAction act)
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
            if (act != null) btn.onClick.AddListener(() => { PlayClickSound(); act(); });
            var lbl = Go("T", g.transform);
            var lRt = lbl.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one; lRt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = fSz; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = tc; t.raycastTarget = false;
        }

        /// <summary>Stat grid box with accent top border, label, and value.</summary>
        void GridStat(Transform p, float x, float y, string label, string val, float w, float h)
        {
            var box = Go("Stat", p);
            var rt = box.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            box.AddComponent<Image>().color = STAT_BG;

            var top = Go("Border", box.transform);
            var tbRt = top.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0, 1); tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(0.5f, 1); tbRt.sizeDelta = new Vector2(0, 2);
            top.AddComponent<Image>().color = ACCENT;

            var lGo = Go("L", box.transform);
            var lRt = lGo.AddComponent<RectTransform>();
            lRt.anchorMin = lRt.anchorMax = new Vector2(0.5f, 1f);
            lRt.pivot = new Vector2(0.5f, 1f); lRt.sizeDelta = new Vector2(w - 10, 13);
            lRt.anchoredPosition = new Vector2(0, -4);
            var lT = lGo.AddComponent<TextMeshProUGUI>();
            lT.text = label; lT.fontSize = 7f; lT.color = TXT_DIM;
            lT.alignment = TextAlignmentOptions.Center; lT.raycastTarget = false;

            var vGo = Go("V", box.transform);
            var vRt = vGo.AddComponent<RectTransform>();
            vRt.anchorMin = vRt.anchorMax = new Vector2(0.5f, 0f);
            vRt.pivot = new Vector2(0.5f, 0f); vRt.sizeDelta = new Vector2(w - 10, 20);
            vRt.anchoredPosition = new Vector2(0, 3);
            var vT = vGo.AddComponent<TextMeshProUGUI>();
            vT.text = val; vT.fontSize = 11f; vT.fontStyle = FontStyles.Bold;
            vT.alignment = TextAlignmentOptions.Center; vT.color = GOLD; vT.raycastTarget = false;
        }
    }
}
