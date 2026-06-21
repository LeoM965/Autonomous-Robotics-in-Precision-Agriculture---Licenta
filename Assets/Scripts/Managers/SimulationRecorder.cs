using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Managers
{
    [System.Serializable]
    public class SavedSequence
    {
        public string name;
        public List<byte[]> rawFrames;
        public string timestamp;
        public Rect cropRect;
        // Audio data
        public float[] audioSamples;
        public int audioSampleRate;
        public int audioChannels;
        public int width;
        public int height;
    }

    public class SimulationRecorder : MonoBehaviour
    {
        private static SimulationRecorder instance;
        public static SimulationRecorder Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = GameObject.Find("SimulationRecorder");
                    if (go == null)
                    {
                        go = new GameObject("SimulationRecorder");
                        instance = go.AddComponent<SimulationRecorder>();
                    }
                    else
                    {
                        instance = go.GetComponent<SimulationRecorder>();
                        if (instance == null)
                            instance = go.AddComponent<SimulationRecorder>();
                    }
                }
                return instance;
            }
        }

        [Header("Settings")]
        public int captureFPS = 24; // 24 FPS for smoother, clearer video
        public int maxRollingFrames = 1440; // Limit to 60 seconds at 24 FPS (prevents OutOfMemory)
        public bool autoRecordBackground = false; // Disabled by default to prevent stutters/memory issues

        private int frameWidth;
        private int frameHeight;

        private float captureInterval => 1f / captureFPS;

        // Dynamic circular buffer - raw pixel data as byte arrays (memory efficient)
        private byte[][] rollingFrameData;
        private int head = 0;
        private int tail = 0;
        private readonly object rollingFrameDataLock = new object();
        private int filledFrameCount = 0;
        private float lastCaptureTime;
        private bool isCaptureRunning = false;
        private float nextAudioListenerCheckTime = 0f;

        private RenderTexture[] rtBuffers;
        private RenderTexture downscaledRt;
        private int currentRtBufferIdx = 0;
        private bool useAsyncReadback = false;

        // Reusable textures - never destroyed/recreated per frame
        private Texture2D displayTexture;
        private Texture2D captureTexture;

        // Cached reference to avoid FindFirstObjectByType every frame
        private UI.Menus.PauseMenu cachedPauseMenu;

        // Saved sequences
        public List<SavedSequence> savedSequences = new List<SavedSequence>();

        private bool isRecording = true;

        // Manual recording state
        private bool isManualRecording = false;
        public bool IsManualRecording => isManualRecording;

        private int lastDecompressedFrameIdx = -1;
        private int lastDecompressedSequenceIdx = -1;
        private int lastDecompressedSeqFrameIdx = -1;
        private Texture2D seqDisplayTexture;

        // Audio components
        private AudioBufferRecorder audioRecorder;
        private AudioSource replayAudioSource;

        public void ResetDecompressionCache()
        {
            lastDecompressedFrameIdx = -1;
            lastDecompressedSequenceIdx = -1;
            lastDecompressedSeqFrameIdx = -1;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBuffer();

            replayAudioSource = GetComponent<AudioSource>();
            if (replayAudioSource == null)
                replayAudioSource = gameObject.AddComponent<AudioSource>();
            replayAudioSource.playOnAwake = false;
            replayAudioSource.loop = false;
            replayAudioSource.ignoreListenerPause = true;
            replayAudioSource.spatialBlend = 0f;
            replayAudioSource.volume = 1f;
            replayAudioSource.mute = false;
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            ClearBuffer();
            EnsureAudioListenerExists();
            StartCoroutine(CaptureInitialFrame());
        }

        public void ClearBuffer()
        {
            lock (rollingFrameDataLock)
            {
                if (rollingFrameData != null)
                {
                    System.Array.Clear(rollingFrameData, 0, rollingFrameData.Length);
                }
                head = 0;
                tail = 0;
                filledFrameCount = 0;
            }
            ResetDecompressionCache();
            Debug.Log("[SimulationRecorder] Buffer cleared.");
        }

        private void CalculateTargetDimensions(int screenWidth, int screenHeight, out int targetWidth, out int targetHeight)
        {
            // Cap height to 1080p to provide higher resolution and better quality
            const int maxTargetHeight = 1080;
            if (screenHeight > maxTargetHeight)
            {
                float scale = (float)maxTargetHeight / screenHeight;
                targetWidth = Mathf.RoundToInt(screenWidth * scale);
                targetHeight = maxTargetHeight;
                
                // Ensure width is even
                if (targetWidth % 2 != 0) targetWidth--;
            }
            else
            {
                targetWidth = screenWidth;
                targetHeight = screenHeight;
            }
        }

        private void InitializeBuffer()
        {
            CalculateTargetDimensions(Screen.width, Screen.height, out frameWidth, out frameHeight);
            useAsyncReadback = SystemInfo.supportsAsyncGPUReadback;

            lock (rollingFrameDataLock)
            {
                rollingFrameData = new byte[maxRollingFrames][];
                head = 0;
                tail = 0;
                filledFrameCount = 0;
            }

            displayTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
            displayTexture.filterMode = FilterMode.Bilinear;

            // Always initialize RenderTexture buffers to support downscaling in both paths
            InitializeRtBuffers();
            
            if (downscaledRt != null) downscaledRt.Release();
            downscaledRt = new RenderTexture(frameWidth, frameHeight, 0, RenderTextureFormat.ARGB32);
            downscaledRt.filterMode = FilterMode.Bilinear;
            downscaledRt.Create();

            if (!useAsyncReadback)
            {
                captureTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
            }

            EnsureAudioListenerExists();
        }

        private void InitializeRtBuffers()
        {
            if (rtBuffers != null)
            {
                for (int i = 0; i < rtBuffers.Length; i++)
                {
                    if (rtBuffers[i] != null) rtBuffers[i].Release();
                }
            }
            rtBuffers = new RenderTexture[3];
            for (int i = 0; i < 3; i++)
            {
                // Capture at native screen width/height to avoid cropping panels
                rtBuffers[i] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                rtBuffers[i].filterMode = FilterMode.Bilinear;
                rtBuffers[i].Create();
            }
        }

        private void ReinitializeForNewSize(int newWidth, int newHeight)
        {
            CalculateTargetDimensions(newWidth, newHeight, out frameWidth, out frameHeight);

            lock (rollingFrameDataLock)
            {
                rollingFrameData = new byte[maxRollingFrames][];
                head = 0;
                tail = 0;
                filledFrameCount = 0;
            }

            if (displayTexture != null) Destroy(displayTexture);
            displayTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
            displayTexture.filterMode = FilterMode.Bilinear;

            InitializeRtBuffers();
            
            if (downscaledRt != null) downscaledRt.Release();
            downscaledRt = new RenderTexture(frameWidth, frameHeight, 0, RenderTextureFormat.ARGB32);
            downscaledRt.filterMode = FilterMode.Bilinear;
            downscaledRt.Create();

            if (!useAsyncReadback)
            {
                if (captureTexture != null) Destroy(captureTexture);
                captureTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
            }
        }

        private void OnDestroy()
        {
            if (displayTexture != null) Destroy(displayTexture);
            if (captureTexture != null) Destroy(captureTexture);
            if (seqDisplayTexture != null) Destroy(seqDisplayTexture);
            if (downscaledRt != null) downscaledRt.Release();
            if (rtBuffers != null)
            {
                for (int i = 0; i < rtBuffers.Length; i++)
                {
                    if (rtBuffers[i] != null) rtBuffers[i].Release();
                }
            }
        }

        public void SetRecordingActive(bool active) { isRecording = active; }
        public bool IsRecordingActive() { return isRecording; }

        private void EnsureAudioListenerExists()
        {
            // Find an active and enabled AudioListener
            AudioListener activeListener = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Exclude);
            if (activeListener == null)
            {
                // No active AudioListener. Check if we can enable an inactive one on an active GameObject
                AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var listener in allListeners)
                {
                    if (!listener.enabled && listener.gameObject.activeInHierarchy)
                    {
                        listener.enabled = true;
                        Debug.Log($"[AudioListener] Enabled existing AudioListener on active GameObject: {listener.gameObject.name}");
                        activeListener = listener;
                        break;
                    }
                }
            }

            if (activeListener == null)
            {
                // If still none, try to add/enable on active main camera
                Camera mainCam = Camera.main;
                if (mainCam != null && mainCam.gameObject.activeInHierarchy)
                {
                    AudioListener listener = mainCam.GetComponent<AudioListener>();
                    if (listener == null)
                    {
                        listener = mainCam.gameObject.AddComponent<AudioListener>();
                        Debug.Log($"[AudioListener] Added AudioListener to active Main Camera: {mainCam.gameObject.name}");
                    }
                    else
                    {
                        listener.enabled = true;
                        Debug.Log($"[AudioListener] Enabled existing AudioListener on active Main Camera: {mainCam.gameObject.name}");
                    }
                    activeListener = listener;
                }
            }

            if (activeListener == null)
            {
                // Try any active camera
                Camera[] activeCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var cam in activeCameras)
                {
                    if (cam.gameObject.activeInHierarchy && cam.targetTexture == null)
                    {
                        AudioListener listener = cam.GetComponent<AudioListener>();
                        if (listener == null)
                        {
                            listener = cam.gameObject.AddComponent<AudioListener>();
                            Debug.Log($"[AudioListener] Added AudioListener to active Camera: {cam.gameObject.name}");
                        }
                        else
                        {
                            listener.enabled = true;
                            Debug.Log($"[AudioListener] Enabled existing AudioListener on active Camera: {cam.gameObject.name}");
                        }
                        activeListener = listener;
                        break;
                    }
                }
            }

            if (activeListener == null)
            {
                // Fallback
                GameObject audioListenerGo = GameObject.Find("FallbackAudioListener");
                if (audioListenerGo == null)
                {
                    audioListenerGo = new GameObject("FallbackAudioListener");
                }
                audioListenerGo.SetActive(true);
                AudioListener fallbackListener = audioListenerGo.GetComponent<AudioListener>();
                if (fallbackListener == null)
                {
                    fallbackListener = audioListenerGo.AddComponent<AudioListener>();
                }
                fallbackListener.enabled = true;
                Debug.Log("[AudioListener] Created/Enabled active fallback AudioListener GameObject.");
                activeListener = fallbackListener;
            }

            // Ensure AudioBufferRecorder is attached
            if (activeListener != null)
            {
                audioRecorder = activeListener.GetComponent<AudioBufferRecorder>();
                bool isNew = false;
                if (audioRecorder == null)
                {
                    audioRecorder = activeListener.gameObject.AddComponent<AudioBufferRecorder>();
                    Debug.Log($"[AudioListener] Added AudioBufferRecorder component to {activeListener.gameObject.name}");
                    isNew = true;

                    // Force Unity's audio DSP graph to rebuild and recognize OnAudioFilterRead on the AudioListener
                    activeListener.enabled = false;
                    activeListener.enabled = true;
                }

                if (isManualRecording && (isNew || !audioRecorder.IsRecording))
                {
                    audioRecorder.StartRecording();
                    Debug.Log("[AudioListener] Restarted audio recording on new/active AudioListener.");
                }
            }
        }
        private void Update()
        {
            if (cachedPauseMenu == null)
                cachedPauseMenu = FindFirstObjectByType<UI.Menus.PauseMenu>();

            bool isMenuOpen = cachedPauseMenu != null && cachedPauseMenu.IsOpen;

            // Auto-stop manual recording if it exceeds 1 minute limit (maxRollingFrames)
            if (isManualRecording && GetFrameCount() >= maxRollingFrames)
            {
                StopManualRecording();
                Debug.Log("[SimulationRecorder] Auto-stopped manual recording at 1 minute limit.");
            }

            // Check for manual recording toggle key
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (isManualRecording)
                {
                    StopManualRecording();
                }
                else
                {
                    StartManualRecording();
                }
            }

            // Only capture when game is running (not paused, not in menu)
            // isCaptureRunning prevents overlapping coroutines
            bool shouldCapture = isManualRecording || (autoRecordBackground && isRecording);
            if (shouldCapture && !isMenuOpen && Time.timeScale > 0 && !isCaptureRunning)
            {
                if (Time.realtimeSinceStartup - lastCaptureTime >= captureInterval)
                {
                    lastCaptureTime = Time.realtimeSinceStartup;
                    isCaptureRunning = true;
                    StartCoroutine(CaptureFrameCoroutine());
                }
            }

            // Ensure there is always one AudioListener in the scene
            if (Time.unscaledTime >= nextAudioListenerCheckTime)
            {
                nextAudioListenerCheckTime = Time.unscaledTime + 2.0f; // Check every 2 seconds
                EnsureAudioListenerExists();
            }
        }

        private void CaptureCameraFrame(RenderTexture targetRt)
        {
            // Capture the full screen to include all HUD/UI panels
            ScreenCapture.CaptureScreenshotIntoRenderTexture(targetRt);
        }

        public void CaptureSingleFrameImmediately()
        {
            StartCoroutine(CaptureFrameCoroutineInternal(false));
        }

        private IEnumerator CaptureInitialFrame()
        {
            yield return new WaitForEndOfFrame();
            yield return CaptureFrameCoroutineInternal(false);
        }

        private IEnumerator CaptureFrameCoroutine()
        {
            yield return new WaitForEndOfFrame();
            yield return CaptureFrameCoroutineInternal(true);
            isCaptureRunning = false;
        }

        private IEnumerator CaptureFrameCoroutineInternal(bool checkMenu)
        {
            if (checkMenu)
            {
                bool isMenuOpen = cachedPauseMenu != null && cachedPauseMenu.IsOpen;
                if (isMenuOpen) yield break;
            }

            try
            {
                int sw = Screen.width;
                int sh = Screen.height;

                // Handle screen resize
                int targetW, targetH;
                CalculateTargetDimensions(sw, sh, out targetW, out targetH);
                if (targetW != frameWidth || targetH != frameHeight)
                    ReinitializeForNewSize(sw, sh);

                int bytesPerFrame = frameWidth * frameHeight * 3;

                int rtIdx = currentRtBufferIdx;
                currentRtBufferIdx = (currentRtBufferIdx + 1) % rtBuffers.Length;
                RenderTexture targetRt = rtBuffers[rtIdx];

                CaptureCameraFrame(targetRt);

                // Downscale targetRt (full screen) to downscaledRt using GPU Blit
                Graphics.Blit(targetRt, downscaledRt);

                if (useAsyncReadback)
                {
                    AsyncGPUReadback.Request(downscaledRt, 0, TextureFormat.RGB24, (AsyncGPUReadbackRequest request) =>
                    {
                        if (request.hasError)
                        {
                            Debug.LogError("[SimulationRecorder] AsyncGPUReadback failed.");
                            return;
                        }

                        var data = request.GetData<byte>();
                        if (data.IsCreated && data.Length == bytesPerFrame)
                        {
                            byte[] frameBytes = new byte[bytesPerFrame];
                            data.CopyTo(frameBytes);
                            
                            // Flip vertically if textures start at top-left (Direct3D, Metal)
                            var device = SystemInfo.graphicsDeviceType;
                            bool shouldFlip = device == GraphicsDeviceType.Direct3D11 || 
                                              device == GraphicsDeviceType.Direct3D12 || 
                                              device == GraphicsDeviceType.Metal;
                            
                            // Offload CPU flip and compression to background thread to prevent stutters
                            System.Threading.Tasks.Task.Run(() =>
                            {
                                if (shouldFlip)
                                {
                                    FlipRawRGB24Vertically(frameBytes, frameWidth, frameHeight);
                                }
                                byte[] compressed = CompressBytes(frameBytes);
                                lock (rollingFrameDataLock)
                                {
                                    if (rollingFrameData != null && rollingFrameData.Length > 0)
                                    {
                                        int length = rollingFrameData.Length;
                                        rollingFrameData[tail] = compressed;
                                        tail = (tail + 1) % length;
                                        if (filledFrameCount < length)
                                        {
                                            filledFrameCount++;
                                        }
                                        else
                                        {
                                            head = (head + 1) % length;
                                        }
                                    }
                                }
                            });
                        }
                    });
                }
                else
                {
                    RenderTexture.active = downscaledRt;
                    captureTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0, false);
                    captureTexture.Apply(false);
                    RenderTexture.active = null;

                    byte[] rawData = captureTexture.GetRawTextureData();
                    if (rawData.Length == bytesPerFrame)
                    {
                        byte[] frameBytes = new byte[bytesPerFrame];
                        Buffer.BlockCopy(rawData, 0, frameBytes, 0, bytesPerFrame);
                        
                        // Offload compression to background thread
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            byte[] compressed = CompressBytes(frameBytes);
                            lock (rollingFrameDataLock)
                            {
                                if (rollingFrameData != null && rollingFrameData.Length > 0)
                                {
                                    int length = rollingFrameData.Length;
                                    rollingFrameData[tail] = compressed;
                                    tail = (tail + 1) % length;
                                    if (filledFrameCount < length)
                                    {
                                        filledFrameCount++;
                                    }
                                    else
                                    {
                                        head = (head + 1) % length;
                                    }
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SimulationRecorder] Capture error: {ex.Message}");
            }
        }

        private void FlipRawRGB24Vertically(byte[] data, int width, int height)
        {
            int rowLength = width * 3;
            byte[] tempRow = new byte[rowLength];

            int halfHeight = height / 2;

            for (int y = 0; y < halfHeight; y++)
            {
                int topOffset = y * rowLength;
                int bottomOffset = (height - 1 - y) * rowLength;

                Buffer.BlockCopy(data, topOffset, tempRow, 0, rowLength);
                Buffer.BlockCopy(data, bottomOffset, data, topOffset, rowLength);
                Buffer.BlockCopy(tempRow, 0, data, bottomOffset, rowLength);
            }
        }

        private byte[] CompressBytes(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Fastest))
                {
                    deflate.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        private byte[] DecompressBytes(byte[] co, int width, int height)
        {
            int decompressedLength = width * height * 3;
            byte[] decompressed = new byte[decompressedLength];
            using (var input = new MemoryStream(co))
            {
                using (var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    int bytesRead = 0;
                    while (bytesRead < decompressedLength)
                    {
                        int read = deflate.Read(decompressed, bytesRead, decompressedLength - bytesRead);
                        if (read <= 0) break;
                        bytesRead += read;
                    }
                }
            }
            return decompressed;
        }

        /// <summary>
        /// Returns a Texture2D for the given ordered frame index in the rolling buffer.
        /// The returned texture is reused internally - do NOT cache it between frames.
        /// </summary>
        public Texture2D GetFrameTexture(int orderedIndex)
        {
            if (filledFrameCount == 0) return null;

            int actualIndex = Mathf.Clamp(orderedIndex, 0, filledFrameCount - 1);

            if (actualIndex == lastDecompressedFrameIdx && displayTexture != null && displayTexture.width == frameWidth && displayTexture.height == frameHeight)
            {
                return displayTexture;
            }

            byte[] compressedData;
            lock (rollingFrameDataLock)
            {
                if (rollingFrameData == null || actualIndex >= filledFrameCount) return null;
                int bufferIndex = (head + actualIndex) % rollingFrameData.Length;
                compressedData = rollingFrameData[bufferIndex];
            }
            byte[] decompressed = DecompressBytes(compressedData, frameWidth, frameHeight);

            if (displayTexture == null || displayTexture.width != frameWidth || displayTexture.height != frameHeight)
            {
                if (displayTexture != null) Destroy(displayTexture);
                displayTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
                displayTexture.filterMode = FilterMode.Trilinear;
                displayTexture.anisoLevel = 9;
            }

            displayTexture.LoadRawTextureData(decompressed);
            displayTexture.Apply(false);

            lastDecompressedFrameIdx = actualIndex;
            return displayTexture;
        }

        /// <summary>
        /// Returns a Texture2D for the given saved sequence index and frame index.
        /// The returned texture is reused internally - do NOT cache it between frames.
        /// </summary>
        public Texture2D GetSavedSequenceFrameTexture(int sequenceIdx, int frameIdx)
        {
            if (sequenceIdx < 0 || sequenceIdx >= savedSequences.Count) return null;
            var seq = savedSequences[sequenceIdx];
            if (seq.rawFrames == null || seq.rawFrames.Count == 0) return null;

            int actualIndex = Mathf.Clamp(frameIdx, 0, seq.rawFrames.Count - 1);

            if (sequenceIdx == lastDecompressedSequenceIdx && actualIndex == lastDecompressedSeqFrameIdx && seqDisplayTexture != null && seqDisplayTexture.width == seq.width && seqDisplayTexture.height == seq.height)
            {
                return seqDisplayTexture;
            }

            byte[] compressedData = seq.rawFrames[actualIndex];
            byte[] decompressed = DecompressBytes(compressedData, seq.width, seq.height);

            if (seqDisplayTexture == null || seqDisplayTexture.width != seq.width || seqDisplayTexture.height != seq.height)
            {
                if (seqDisplayTexture != null) Destroy(seqDisplayTexture);
                seqDisplayTexture = new Texture2D(seq.width, seq.height, TextureFormat.RGB24, false);
                seqDisplayTexture.filterMode = FilterMode.Trilinear;
                seqDisplayTexture.anisoLevel = 9;
            }

            seqDisplayTexture.LoadRawTextureData(decompressed);
            seqDisplayTexture.Apply(false);

            lastDecompressedSequenceIdx = sequenceIdx;
            lastDecompressedSeqFrameIdx = actualIndex;

            return seqDisplayTexture;
        }

        /// <summary>Returns the number of frames currently in the rolling buffer.</summary>
        public int GetFrameCount() { return filledFrameCount; }

        /// <summary>
        /// Saves a sequence of full-resolution frames from the rolling buffer.
        /// Frames are stored as compressed byte arrays in the rawFrames list.
        /// </summary>
        public void SaveSequence(string name, int startIndex, int endIndex, Rect cropRect)
        {
            int totalFrames = filledFrameCount;
            if (totalFrames == 0) return;

            startIndex = Mathf.Clamp(startIndex, 0, totalFrames - 1);
            endIndex = Mathf.Clamp(endIndex, startIndex, totalFrames - 1);
            int count = endIndex - startIndex + 1;

            List<byte[]> seqRawFrames = new List<byte[]>(count);
            for (int i = 0; i < count; i++)
            {
                int srcIdx = startIndex + i;
                byte[] comp = null;
                lock (rollingFrameDataLock)
                {
                    if (rollingFrameData != null && srcIdx < filledFrameCount)
                    {
                        int bufferIndex = (head + srcIdx) % rollingFrameData.Length;
                        comp = rollingFrameData[bufferIndex];
                    }
                }
                if (comp != null)
                {
                    seqRawFrames.Add(comp);
                }
            }

            // Capture audio samples
            float[] audioSamples = null;
            int rate = 44100;
            int chans = 2;
            if (audioRecorder != null)
            {
                audioSamples = audioRecorder.GetRecordedAudio();
                rate = audioRecorder.SampleRate;
                chans = audioRecorder.Channels;
            }

            SavedSequence seq = new SavedSequence
            {
                name = string.IsNullOrEmpty(name) ? $"Secvență_{savedSequences.Count + 1}" : name,
                rawFrames = seqRawFrames,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                cropRect = cropRect,
                audioSamples = audioSamples,
                audioSampleRate = rate,
                audioChannels = chans,
                width = frameWidth,
                height = frameHeight
            };

            savedSequences.Add(seq);
        }

        public void DeleteSequence(int index)
        {
            if (index < 0 || index >= savedSequences.Count) return;

            savedSequences.RemoveAt(index);
            ResetDecompressionCache();
        }

        private Texture2D CropTexture(Texture2D original, Rect normalizedCrop)
        {
            int x = Mathf.RoundToInt(normalizedCrop.x * original.width);
            int y = Mathf.RoundToInt(normalizedCrop.y * original.height);
            int width = Mathf.RoundToInt(normalizedCrop.width * original.width);
            int height = Mathf.RoundToInt(normalizedCrop.height * original.height);

            // Clamp values to ensure they are within the texture bounds
            x = Mathf.Clamp(x, 0, original.width);
            y = Mathf.Clamp(y, 0, original.height);
            width = Mathf.Clamp(width, 1, original.width - x);
            height = Mathf.Clamp(height, 1, original.height - y);

            Texture2D cropped = new Texture2D(width, height, original.format, false);
            
            // Get pixels from original and set to cropped
            Color[] pixels = original.GetPixels(x, y, width, height);
            cropped.SetPixels(pixels);
            cropped.Apply();
            
            return cropped;
        }

        /// <summary>
        /// Exports full-resolution frames to disk as JPG files.
        /// </summary>
        public string ExportSequenceToDisk(int index)
        {
            if (index < 0 || index >= savedSequences.Count) return null;

            var seq = savedSequences[index];
            string folderName = seq.name.Replace(" ", "_");
            string path = Path.Combine(Application.dataPath, "..", "Exported_Recordings", folderName);

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Texture2D tempTex = new Texture2D(seq.width, seq.height, TextureFormat.RGB24, false);

                for (int i = 0; i < seq.rawFrames.Count; i++)
                {
                    byte[] comp = seq.rawFrames[i];
                    byte[] decomp = DecompressBytes(comp, seq.width, seq.height);
                    tempTex.LoadRawTextureData(decomp);
                    tempTex.Apply(false);

                    byte[] bytes;
                    
                    // Apply crop if it is not full screen
                    if (seq.cropRect.width < 0.99f || seq.cropRect.height < 0.99f || seq.cropRect.x > 0.01f || seq.cropRect.y > 0.01f)
                    {
                        Texture2D croppedFrame = CropTexture(tempTex, seq.cropRect);
                        bytes = croppedFrame.EncodeToJPG(98);
                        Destroy(croppedFrame); // Clean up temporary cropped texture
                    }
                    else
                    {
                        bytes = tempTex.EncodeToJPG(98);
                    }

                    string filePath = Path.Combine(path, $"frame_{i:D4}.jpg");
                    File.WriteAllBytes(filePath, bytes);
                }

                Destroy(tempTex);

                string metaPath = Path.Combine(path, "info.txt");
                string metadata = $"Name: {seq.name}\n" +
                                  $"Timestamp: {seq.timestamp}\n" +
                                  $"Frames: {seq.rawFrames.Count}\n" +
                                  $"Resolution: {seq.width}x{seq.height}\n" +
                                  $"FPS: {captureFPS}\n";
                File.WriteAllText(metaPath, metadata);

                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SimulationRecorder] Export error: {ex.Message}");
                return null;
            }
        }

        public void StartManualRecording()
        {
            if (isManualRecording) return;
            
            EnsureAudioListenerExists();
            ClearBuffer();
            ResetDecompressionCache();
            isManualRecording = true;

            if (audioRecorder != null)
                audioRecorder.StartRecording();
            
            Debug.Log("[SimulationRecorder] Manual recording started.");
        }

        public void StopManualRecording(string sequenceName = "")
        {
            if (!isManualRecording) return;
            
            if (audioRecorder != null)
                audioRecorder.StopRecording();

            isManualRecording = false;
            
            int frameCount = GetFrameCount();
            if (frameCount > 0)
            {
                string name = string.IsNullOrEmpty(sequenceName) 
                    ? $"Secvență_Manuală_{savedSequences.Count + 1}" 
                    : sequenceName;
                
                SaveSequence(name, 0, frameCount - 1, new Rect(0f, 0f, 1f, 1f));
                Debug.Log($"[SimulationRecorder] Manual recording stopped and saved as: {name}");
            }
            else
            {
                Debug.LogWarning("[SimulationRecorder] Manual recording stopped, but no frames were captured.");
            }
            
            ClearBuffer();
            ResetDecompressionCache();
        }

        public void PlayReplayAudio(int sequenceIndex, int startFrame)
        {
            if (sequenceIndex < 0 || sequenceIndex >= savedSequences.Count) return;
            var seq = savedSequences[sequenceIndex];
            if (seq.audioSamples == null || seq.audioSamples.Length == 0) return;

            if (replayAudioSource == null)
            {
                replayAudioSource = GetComponent<AudioSource>();
                if (replayAudioSource == null)
                    replayAudioSource = gameObject.AddComponent<AudioSource>();
                replayAudioSource.playOnAwake = false;
                replayAudioSource.loop = false;
                replayAudioSource.ignoreListenerPause = true;
                replayAudioSource.spatialBlend = 0f;
                replayAudioSource.volume = 1f;
                replayAudioSource.mute = false;
            }

            if (replayAudioSource.clip == null || replayAudioSource.clip.name != seq.name)
            {
                AudioClip clip = AudioClip.Create(seq.name, seq.audioSamples.Length / seq.audioChannels, seq.audioChannels, seq.audioSampleRate, false);
                clip.SetData(seq.audioSamples, 0);
                replayAudioSource.clip = clip;
            }

            replayAudioSource.time = Mathf.Clamp((float)startFrame / captureFPS, 0f, replayAudioSource.clip.length - 0.05f);
            replayAudioSource.Play();
        }

        public void PauseReplayAudio()
        {
            if (replayAudioSource != null && replayAudioSource.isPlaying)
                replayAudioSource.Pause();
        }

        public void StopReplayAudio()
        {
            if (replayAudioSource != null)
                replayAudioSource.Stop();
        }

        public void SetReplayAudioTime(float time)
        {
            if (replayAudioSource != null && replayAudioSource.clip != null)
            {
                replayAudioSource.time = Mathf.Clamp(time, 0f, replayAudioSource.clip.length - 0.05f);
            }
        }

        private void OnGUI()
        {
            if (isManualRecording)
            {
                // Flashing effect (flashes once per second)
                bool showRec = Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0;
                
                if (showRec)
                {
                    GUIStyle recStyle = new GUIStyle();
                    recStyle.fontSize = 20;
                    recStyle.fontStyle = FontStyle.Bold;
                    recStyle.normal.textColor = Color.red;
                    
                    // Shadow
                    GUIStyle shadowStyle = new GUIStyle(recStyle);
                    shadowStyle.normal.textColor = new Color(0, 0, 0, 0.6f);
                    
                    int seconds = Mathf.FloorToInt((float)GetFrameCount() / captureFPS);
                    int minutes = seconds / 60;
                    seconds %= 60;
                    string durationText = $"● REC  {minutes:D2}:{seconds:D2}";
                    
                    GUI.Label(new Rect(Screen.width - 160, 22, 150, 30), durationText, shadowStyle);
                    GUI.Label(new Rect(Screen.width - 161, 21, 150, 30), durationText, recStyle);
                }
            }
        }
    }

    public class AudioBufferRecorder : MonoBehaviour
    {
        private List<float> audioBuffer = new List<float>();
        private readonly object bufferLock = new object();
        private bool isRecording = false;
        private int sampleRate;
        private int channelsCount = 2;

        public bool IsRecording => isRecording;

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
        }

        public void StartRecording()
        {
            lock (bufferLock)
            {
                audioBuffer.Clear();
                isRecording = true;
            }
        }

        public void StopRecording()
        {
            lock (bufferLock)
            {
                isRecording = false;
            }
        }

        public float[] GetRecordedAudio()
        {
            lock (bufferLock)
            {
                return audioBuffer.ToArray();
            }
        }

        public int SampleRate => sampleRate;
        public int Channels => channelsCount;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            channelsCount = channels;
            if (!isRecording) return;

            lock (bufferLock)
            {
                audioBuffer.AddRange(data);
            }
        }
    }
}
