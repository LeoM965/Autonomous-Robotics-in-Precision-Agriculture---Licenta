using UnityEngine;
using Managers;
using System.Collections.Generic;

namespace UI.Menus.Tabs
{
    public class RecordingsDashboardTab : BaseDashboardTab
    {
        private int selectedSeqIndex = -1; // -1 for Live Buffer, >= 0 for saved sequences
        
        // Editor Mode State
        private int currentFrameIdx = 0;
        private int startFrameIdx = 0;
        private int endFrameIdx = int.MaxValue;
        private bool isPlaying = false;
        private float playbackStartTime = 0f;
        
        // Crop State
        private bool isCropEnabled = false;
        private float cropLeft = 0.0f;
        private float cropRight = 1.0f;
        private float cropBottom = 0.0f;
        private float cropTop = 1.0f;
        
        private string newSequenceName = "Secvență Nouă";
        private string statusMessage = "";
        private float statusTimer = 0f;

        // Playback Mode State
        private int playbackFrameIdx = 0;
        private bool isPlaybackPlaying = false;

        public void OnOpenTab()
        {
            if (selectedSeqIndex == -1)
            {
                startFrameIdx = 0;
                endFrameIdx = int.MaxValue;
                currentFrameIdx = 0;
            }
        }

        public override void DrawTab(float x, float y, UITheme theme)
        {
            // Verify SimulationRecorder exists
            if (SimulationRecorder.Instance == null)
            {
                GUI.Label(new Rect(x, y, 400, 20), "Managerul de înregistrări nu a fost găsit în scenă.", theme.Bad);
                return;
            }

            // Clear status message after 3 seconds
            if (!string.IsNullOrEmpty(statusMessage) && Time.unscaledTime > statusTimer)
            {
                statusMessage = "";
            }

            // Draw Split View: Left (Saved List), Right (Playback/Editor Window)
            DrawLeftPanel(x, y, theme);
            DrawRightPanel(x + 235, y, theme);
        }

        private void DrawLeftPanel(float x, float y, UITheme theme)
        {
            float panelWidth = 220f;
            float panelHeight = 445f;

            // Draw background panel for Saved recordings list
            MapHelper.DrawBox(new Rect(x, y, panelWidth, panelHeight), new Color(0, 0, 0, 0.2f));
            MapHelper.DrawBorder(new Rect(x, y, panelWidth, panelHeight), theme.panelBorder, 1);

            float py = y + 10;
            GUI.Label(new Rect(x + 10, py, panelWidth - 20, 20), "FILMĂRI DISPONIBILE", theme.Value);
            py += 25;
            MapHelper.DrawBox(new Rect(x + 10, py, panelWidth - 20, 1), new Color(1, 1, 1, 0.1f));
            py += 10;

            var savedSequences = SimulationRecorder.Instance.savedSequences;
            int totalItems = savedSequences.Count + 1; // List contains Live Buffer + all saved sequences

            DrawScrollableArea(x + 10, ref py, panelWidth - 20, 380f, totalItems, 40f, (rowY) =>
            {
                // Item 0: Live Buffer Editor
                Rect liveRect = new Rect(0, rowY, panelWidth - 40, 36);
                bool isLiveSelected = (selectedSeqIndex == -1);
                
                Color itemBg = isLiveSelected ? new Color(theme.panelBorder.r, theme.panelBorder.g, theme.panelBorder.b, 0.25f)
                                             : new Color(1, 1, 1, 0.03f);
                MapHelper.DrawBox(liveRect, itemBg);
                if (isLiveSelected)
                    MapHelper.DrawBorder(liveRect, theme.panelBorder, 1);

                if (GUI.Button(liveRect, "  🔴 ÎNREGISTRARE LIVE\n  (Click pt. editare / crop)", theme.Label))
                {
                    selectedSeqIndex = -1;
                    isPlaying = false;
                    isPlaybackPlaying = false;
                    statusMessage = "";
                    startFrameIdx = 0;
                    endFrameIdx = int.MaxValue;
                    currentFrameIdx = 0;
                    SimulationRecorder.Instance.StopReplayAudio();
                }

                rowY += 40f;

                // Items 1..N: Saved Sequences
                for (int i = 0; i < savedSequences.Count; i++)
                {
                    Rect itemRect = new Rect(0, rowY, panelWidth - 40, 36);
                    bool isSelected = (selectedSeqIndex == i);

                    Color bg = isSelected ? new Color(theme.panelBorder.r, theme.panelBorder.g, theme.panelBorder.b, 0.25f)
                                          : new Color(1, 1, 1, 0.03f);
                    MapHelper.DrawBox(itemRect, bg);
                    if (isSelected)
                        MapHelper.DrawBorder(itemRect, theme.panelBorder, 1);

                    var seq = savedSequences[i];
                    int fps = SimulationRecorder.Instance.captureFPS;
                    string durationStr = FormatTime(seq.rawFrames.Count, fps);
                    string seqLabel = $"  🎥 {seq.name}\n  {durationStr} | {seq.timestamp}";
                    
                    if (GUI.Button(itemRect, seqLabel, theme.Label))
                    {
                        selectedSeqIndex = i;
                        playbackFrameIdx = 0;
                        isPlaybackPlaying = false;
                        isPlaying = false;
                        statusMessage = "";
                        SimulationRecorder.Instance.StopReplayAudio();
                    }

                    rowY += 40f;
                }
            });
        }

        private void DrawRightPanel(float rx, float y, UITheme theme)
        {
            int fps = SimulationRecorder.Instance.captureFPS;
            float panelWidth = 475f;
            float panelHeight = 445f;
            Rect previewRect = new Rect(rx + 15, y + 25, 445, 250);

            // Draw Right Panel Border
            MapHelper.DrawBox(new Rect(rx, y, panelWidth, panelHeight), new Color(0, 0, 0, 0.15f));
            MapHelper.DrawBorder(new Rect(rx, y, panelWidth, panelHeight), theme.panelBorder, 1);

            if (selectedSeqIndex == -1)
            {
                // --- EDITOR MODE: Live Circular Buffer ---
                GUI.Label(new Rect(rx + 15, y + 5, 200, 20), "EDITOR LIVE BUFFER", theme.Value);

                int totalFrames = SimulationRecorder.Instance.GetFrameCount();
                if (totalFrames == 0)
                {
                    MapHelper.DrawBox(previewRect, Color.black);
                    GUI.Label(new Rect(previewRect.x + 20, previewRect.y + 100, 400, 50), 
                        "Nu sunt cadre înregistrate.\nApasă [F9] în timpul simulării pentru a înregistra manual!", theme.Warn);
                    return;
                }

                // Enforce frame indexes bounds
                currentFrameIdx = Mathf.Clamp(currentFrameIdx, 0, totalFrames - 1);
                startFrameIdx = Mathf.Clamp(startFrameIdx, 0, totalFrames - 1);
                endFrameIdx = Mathf.Clamp(endFrameIdx, startFrameIdx, totalFrames - 1);

                // Playback Logic
                if (isPlaying)
                {
                    float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                    float elapsed = Time.unscaledTime - playbackStartTime;
                    int totalPlaybackFrames = endFrameIdx - startFrameIdx + 1;
                    if (totalPlaybackFrames > 0)
                    {
                        int frameOffset = (int)(elapsed / timePerFrame);
                        currentFrameIdx = startFrameIdx + (frameOffset % totalPlaybackFrames);
                    }
                }

                // Get texture for current frame
                Texture2D activeTex = SimulationRecorder.Instance.GetFrameTexture(currentFrameIdx);
                if (activeTex != null)
                {
                    MapHelper.DrawBox(previewRect, Color.black);

                    // Preserve aspect ratio
                    float textureAspect = (float)activeTex.width / activeTex.height;
                    float containerAspect = previewRect.width / previewRect.height;
                    float targetW = previewRect.width;
                    float targetH = previewRect.height;
                    if (textureAspect > containerAspect)
                    {
                        targetW = previewRect.width;
                        targetH = previewRect.width / textureAspect;
                    }
                    else
                    {
                        targetH = previewRect.height;
                        targetW = previewRect.height * textureAspect;
                    }
                    float targetX = previewRect.x + (previewRect.width - targetW) / 2f;
                    float targetY = previewRect.y + (previewRect.height - targetH) / 2f;
                    Rect drawRect = new Rect(targetX, targetY, targetW, targetH);

                    GUI.DrawTextureWithTexCoords(drawRect, activeTex, new Rect(0f, 0f, 1f, 1f));

                    // If spatial crop is enabled, draw visual crop boundary on preview
                    if (isCropEnabled)
                    {
                        float ox = drawRect.x + cropLeft * drawRect.width;
                        float ow = (cropRight - cropLeft) * drawRect.width;
                        float oy = drawRect.y + (1.0f - cropTop) * drawRect.height;
                        float oh = (cropTop - cropBottom) * drawRect.height;
                        
                        MapHelper.DrawBorder(new Rect(ox, oy, ow, oh), Color.green, 2);
                    }
                }

                // Floating Fullscreen Button on top of the video
                if (GUI.Button(new Rect(previewRect.xMax - 120, previewRect.y + 10, 110, 22), "🔎 Ecran Mare", theme.Button))
                {
                    isFullscreenMode = true;
                    isPlaying = false;
                }

                float controlsY = y + 282;

                // Playback Slider (Scrubber)
                GUI.Label(new Rect(rx + 15, controlsY, 40, 16), "Timp:", theme.Label);
                int newFrameIdx = (int)GUI.HorizontalSlider(new Rect(rx + 60, controlsY + 4, 290, 16), currentFrameIdx, 0, totalFrames - 1);
                if (newFrameIdx != currentFrameIdx)
                {
                    currentFrameIdx = newFrameIdx;
                    isPlaying = false;
                }
                string timeStr = $"{FormatTime(currentFrameIdx, fps)} / {FormatTime(totalFrames - 1, fps)}";
                GUI.Label(new Rect(rx + 360, controlsY, 100, 16), timeStr, theme.Value);
                controlsY += 22;

                // Play / Trim Controls
                if (GUI.Button(new Rect(rx + 15, controlsY, 80, 24), isPlaying ? "PAUZĂ" : "REDARE", theme.Button))
                {
                    isPlaying = !isPlaying;
                    if (isPlaying)
                    {
                        if (currentFrameIdx < startFrameIdx || currentFrameIdx > endFrameIdx)
                            currentFrameIdx = startFrameIdx;
                        
                        float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                        playbackStartTime = Time.unscaledTime - (currentFrameIdx - startFrameIdx) * timePerFrame;
                    }
                }

                if (GUI.Button(new Rect(rx + 105, controlsY, 110, 24), "Setează Început", theme.Button))
                {
                    startFrameIdx = currentFrameIdx;
                    if (endFrameIdx < startFrameIdx) endFrameIdx = startFrameIdx;
                }

                if (GUI.Button(new Rect(rx + 225, controlsY, 110, 24), "Setează Sfârșit", theme.Button))
                {
                    endFrameIdx = currentFrameIdx;
                    if (startFrameIdx > endFrameIdx) startFrameIdx = endFrameIdx;
                }

                if (GUI.Button(new Rect(rx + 345, controlsY, 115, 24), "Resetează Limite", theme.Button))
                {
                    startFrameIdx = 0;
                    endFrameIdx = totalFrames - 1;
                }

                controlsY += 30;

                // Crop Controls
                isCropEnabled = GUI.Toggle(new Rect(rx + 15, controlsY, 150, 18), isCropEnabled, "  Decupare Spațială (Crop)", theme.Label);
                
                // Auto background record toggle
                bool autoRec = SimulationRecorder.Instance.autoRecordBackground;
                bool newAutoRec = GUI.Toggle(new Rect(rx + 225, controlsY, 230, 18), autoRec, "  Auto-înregistrare în fundal", theme.Label);
                if (newAutoRec != autoRec)
                {
                    SimulationRecorder.Instance.autoRecordBackground = newAutoRec;
                }

                if (isCropEnabled)
                {
                    controlsY += 22;
                    // Stânga & Dreapta
                    GUI.Label(new Rect(rx + 15, controlsY, 60, 16), "Stânga:", theme.Label);
                    cropLeft = GUI.HorizontalSlider(new Rect(rx + 75, controlsY + 4, 130, 16), cropLeft, 0.0f, cropRight - 0.1f);
                    
                    GUI.Label(new Rect(rx + 245, controlsY, 60, 16), "Dreapta:", theme.Label);
                    cropRight = GUI.HorizontalSlider(new Rect(rx + 305, controlsY + 4, 130, 16), cropRight, cropLeft + 0.1f, 1.0f);

                    controlsY += 20;

                    // Jos & Sus
                    GUI.Label(new Rect(rx + 15, controlsY, 60, 16), "Jos:", theme.Label);
                    cropBottom = GUI.HorizontalSlider(new Rect(rx + 75, controlsY + 4, 130, 16), cropBottom, 0.0f, cropTop - 0.1f);
                    
                    GUI.Label(new Rect(rx + 245, controlsY, 60, 16), "Sus:", theme.Label);
                    cropTop = GUI.HorizontalSlider(new Rect(rx + 305, controlsY + 4, 130, 16), cropTop, cropBottom + 0.1f, 1.0f);
                }
                else
                {
                    controlsY += 40; // Spacing fallback
                }

                controlsY += 22;

                // Save Area
                MapHelper.DrawBox(new Rect(rx + 15, controlsY, panelWidth - 30, 1), new Color(1, 1, 1, 0.1f));
                controlsY += 10;

                GUI.Label(new Rect(rx + 15, controlsY, 130, 24), "Nume Secvență:", theme.Label);
                newSequenceName = GUI.TextField(new Rect(rx + 130, controlsY, 170, 24), newSequenceName, theme.Input);

                if (GUI.Button(new Rect(rx + 310, controlsY, 150, 24), "Salvează Filmare", theme.Button))
                {
                    Rect finalCropRect = isCropEnabled 
                        ? new Rect(cropLeft, cropBottom, cropRight - cropLeft, cropTop - cropBottom)
                        : new Rect(0f, 0f, 1f, 1f);

                    SimulationRecorder.Instance.SaveSequence(newSequenceName, startFrameIdx, endFrameIdx, finalCropRect);
                    statusMessage = "Secvență salvată cu succes!";
                    statusTimer = Time.unscaledTime + 3f;
                    
                    // Reset fields
                    newSequenceName = $"Secvență_{SimulationRecorder.Instance.savedSequences.Count + 1}";
                }

                if (!string.IsNullOrEmpty(statusMessage))
                {
                    GUI.Label(new Rect(rx + 15, controlsY - 30, 200, 20), statusMessage, theme.Good);
                }
            }
            else
            {
                // --- PLAYBACK MODE: Replaying Saved Sequences ---
                var savedSequences = SimulationRecorder.Instance.savedSequences;
                if (selectedSeqIndex >= savedSequences.Count)
                {
                    selectedSeqIndex = -1;
                    return;
                }

                var seq = savedSequences[selectedSeqIndex];
                GUI.Label(new Rect(rx + 15, y + 5, 60, 20), "Nume:", theme.Label);
                string newName = GUI.TextField(new Rect(rx + 80, y + 3, 200, 20), seq.name, theme.Input);
                if (newName != seq.name)
                {
                    seq.name = newName;
                }

                if (seq.rawFrames == null || seq.rawFrames.Count == 0)
                {
                    MapHelper.DrawBox(previewRect, Color.black);
                    GUI.Label(new Rect(previewRect.x + 50, previewRect.y + 110, 350, 30), 
                        "Eroare: Secvența selectată nu conține cadre.", theme.Bad);
                    return;
                }

                // Playback logic
                if (isPlaybackPlaying)
                {
                    float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                    float elapsed = Time.unscaledTime - playbackStartTime;
                    int totalPlaybackFrames = seq.rawFrames.Count;
                    if (totalPlaybackFrames > 0)
                    {
                        int frameOffset = (int)(elapsed / timePerFrame);
                        int newFrame = frameOffset % totalPlaybackFrames;
                        if (newFrame < playbackFrameIdx)
                        {
                            // Looped! Restart audio
                            SimulationRecorder.Instance.PlayReplayAudio(selectedSeqIndex, 0);
                        }
                        playbackFrameIdx = newFrame;
                    }
                }

                playbackFrameIdx = Mathf.Clamp(playbackFrameIdx, 0, seq.rawFrames.Count - 1);

                // Draw Cropped Image using Texture UV coordinates
                Texture2D activeTex = SimulationRecorder.Instance.GetSavedSequenceFrameTexture(selectedSeqIndex, playbackFrameIdx);
                if (activeTex != null)
                {
                    MapHelper.DrawBox(previewRect, Color.black);

                    // Preserve aspect ratio of the cropped area
                    float textureAspect = (seq.width * seq.cropRect.width) / (seq.height * seq.cropRect.height);
                    float containerAspect = previewRect.width / previewRect.height;
                    float targetW = previewRect.width;
                    float targetH = previewRect.height;
                    if (textureAspect > containerAspect)
                    {
                        targetW = previewRect.width;
                        targetH = previewRect.width / textureAspect;
                    }
                    else
                    {
                        targetH = previewRect.height;
                        targetW = previewRect.height * textureAspect;
                    }
                    float targetX = previewRect.x + (previewRect.width - targetW) / 2f;
                    float targetY = previewRect.y + (previewRect.height - targetH) / 2f;
                    Rect drawRect = new Rect(targetX, targetY, targetW, targetH);

                    // Display with crop UV coordinates
                    GUI.DrawTextureWithTexCoords(drawRect, activeTex, 
                        new Rect(seq.cropRect.x, seq.cropRect.y, seq.cropRect.width, seq.cropRect.height));
                }

                // Floating Fullscreen Button on top of the video
                if (GUI.Button(new Rect(previewRect.xMax - 120, previewRect.y + 10, 110, 22), "🔎 Ecran Mare", theme.Button))
                {
                    isFullscreenMode = true;
                    isPlaybackPlaying = false;
                    SimulationRecorder.Instance.PauseReplayAudio();
                }

                float controlsY = y + 282;

                // Scrubber slider
                GUI.Label(new Rect(rx + 15, controlsY, 40, 16), "Timp:", theme.Label);
                int newPlaybackIdx = (int)GUI.HorizontalSlider(new Rect(rx + 60, controlsY + 4, 290, 16), playbackFrameIdx, 0, seq.rawFrames.Count - 1);
                if (newPlaybackIdx != playbackFrameIdx)
                {
                    playbackFrameIdx = newPlaybackIdx;
                    isPlaybackPlaying = false;
                    SimulationRecorder.Instance.StopReplayAudio();
                }
                string timeStr = $"{FormatTime(playbackFrameIdx, fps)} / {FormatTime(seq.rawFrames.Count - 1, fps)}";
                GUI.Label(new Rect(rx + 360, controlsY, 100, 16), timeStr, theme.Value);
                
                controlsY += 22;

                // Play/Pause button
                if (GUI.Button(new Rect(rx + 15, controlsY, 80, 24), isPlaybackPlaying ? "PAUZĂ" : "REDARE", theme.Button))
                {
                    isPlaybackPlaying = !isPlaybackPlaying;
                    if (isPlaybackPlaying)
                    {
                        float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                        playbackStartTime = Time.unscaledTime - playbackFrameIdx * timePerFrame;
                        SimulationRecorder.Instance.PlayReplayAudio(selectedSeqIndex, playbackFrameIdx);
                    }
                    else
                    {
                        SimulationRecorder.Instance.PauseReplayAudio();
                    }
                }
                
                controlsY += 35;

                // Metadata Details
                GUI.Label(new Rect(rx + 15, controlsY, 400, 16), $"Durată: {FormatTime(seq.rawFrames.Count, fps)} ({seq.rawFrames.Count} cadre) | Dată salvare: {seq.timestamp}", theme.Label);
                controlsY += 18;
                GUI.Label(new Rect(rx + 15, controlsY, 400, 16), $"Dimensiune crop: {seq.cropRect.width * 100:F0}% x {seq.cropRect.height * 100:F0}% din ecran", theme.Label);

                controlsY += 30;

                // Operations Footer
                MapHelper.DrawBox(new Rect(rx + 15, controlsY, panelWidth - 30, 1), new Color(1, 1, 1, 0.1f));
                controlsY += 10;

                if (GUI.Button(new Rect(rx + 15, controlsY, 130, 26), "Exportă JPG", theme.Button))
                {
                    string path = SimulationRecorder.Instance.ExportSequenceToDisk(selectedSeqIndex);
                    if (!string.IsNullOrEmpty(path))
                    {
                        statusMessage = "Exportat pe disc!";
                        statusTimer = Time.unscaledTime + 4f;
                    }
                    else
                    {
                        statusMessage = "Eroare la export!";
                        statusTimer = Time.unscaledTime + 4f;
                    }
                }

                if (GUI.Button(new Rect(rx + 155, controlsY, 130, 26), "Șterge Filmare", theme.Button))
                {
                    SimulationRecorder.Instance.StopReplayAudio();
                    SimulationRecorder.Instance.DeleteSequence(selectedSeqIndex);
                    selectedSeqIndex = -1;
                    isPlaying = false;
                    isPlaybackPlaying = false;
                    statusMessage = "Secvență ștearsă.";
                    statusTimer = Time.unscaledTime + 3f;
                }

                if (GUI.Button(new Rect(rx + 295, controlsY, 165, 26), "Înapoi la Înregistrare", theme.Button))
                {
                    selectedSeqIndex = -1;
                    isPlaying = false;
                    isPlaybackPlaying = false;
                    statusMessage = "";
                    SimulationRecorder.Instance.StopReplayAudio();
                }

                if (!string.IsNullOrEmpty(statusMessage))
                {
                    GUI.Label(new Rect(rx + 15, controlsY + 30, 440, 20), statusMessage, theme.Good);
                }
            }
        }

        public bool IsFullscreenMode => isFullscreenMode;
        private bool isFullscreenMode = false;

        public void DrawFullscreen(UITheme theme)
        {
            if (SimulationRecorder.Instance == null)
            {
                isFullscreenMode = false;
                return;
            }

            int fps = SimulationRecorder.Instance.captureFPS;

            // Get active texture based on mode
            Texture2D activeTex = null;
            Rect drawCropRect = new Rect(0f, 0f, 1f, 1f); // Default: full frame, no flip needed
            int totalFrames = 0;

            if (selectedSeqIndex == -1)
            {
                // Editor Mode - Live Buffer
                totalFrames = SimulationRecorder.Instance.GetFrameCount();
                if (totalFrames > 0)
                {
                    currentFrameIdx = Mathf.Clamp(currentFrameIdx, 0, totalFrames - 1);
                    startFrameIdx = Mathf.Clamp(startFrameIdx, 0, totalFrames - 1);
                    endFrameIdx = Mathf.Clamp(endFrameIdx, startFrameIdx, totalFrames - 1);

                    // Playback update
                    if (isPlaying)
                    {
                        float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                        float elapsed = Time.unscaledTime - playbackStartTime;
                        int totalPlaybackFrames = endFrameIdx - startFrameIdx + 1;
                        if (totalPlaybackFrames > 0)
                        {
                            int frameOffset = (int)(elapsed / timePerFrame);
                            currentFrameIdx = startFrameIdx + (frameOffset % totalPlaybackFrames);
                        }
                    }

                    activeTex = SimulationRecorder.Instance.GetFrameTexture(currentFrameIdx);

                    if (isCropEnabled)
                    {
                        drawCropRect = new Rect(cropLeft, cropBottom, cropRight - cropLeft, cropTop - cropBottom);
                    }
                }
            }
            else
            {
                // Playback Mode - Saved Sequence
                var savedSequences = SimulationRecorder.Instance.savedSequences;
                if (selectedSeqIndex < savedSequences.Count)
                {
                    var seq = savedSequences[selectedSeqIndex];
                    totalFrames = seq.rawFrames.Count;
                    playbackFrameIdx = Mathf.Clamp(playbackFrameIdx, 0, totalFrames - 1);

                    // Playback update
                    if (isPlaybackPlaying)
                    {
                        float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                        float elapsed = Time.unscaledTime - playbackStartTime;
                        int totalPlaybackFrames = totalFrames;
                        if (totalPlaybackFrames > 0)
                        {
                            int frameOffset = (int)(elapsed / timePerFrame);
                            playbackFrameIdx = frameOffset % totalPlaybackFrames;
                        }
                    }

                    activeTex = SimulationRecorder.Instance.GetSavedSequenceFrameTexture(selectedSeqIndex, playbackFrameIdx);
                    drawCropRect = new Rect(seq.cropRect.x, seq.cropRect.y, seq.cropRect.width, seq.cropRect.height);
                }
            }

            if (activeTex == null)
            {
                isFullscreenMode = false;
                return;
            }

            // Clear status message after 3 seconds
            if (!string.IsNullOrEmpty(statusMessage) && Time.unscaledTime > statusTimer)
            {
                statusMessage = "";
            }

            // 1. DRAW VIDEO IN THE RIGHT CONTAINER (No overlap, no dead space)
            float vx = 310;
            float vy = 20;
            float vw = Screen.width - 330;
            float vh = Screen.height - 130;

            // Preserve aspect ratio
            float textureAspect = (float)activeTex.width / activeTex.height;
            if (selectedSeqIndex >= 0)
            {
                var seq = SimulationRecorder.Instance.savedSequences[selectedSeqIndex];
                textureAspect = (seq.width * seq.cropRect.width) / (seq.height * seq.cropRect.height);
            }
            float containerAspect = vw / vh;
            float targetW = vw;
            float targetH = vh;
            if (textureAspect > containerAspect)
            {
                targetW = vw;
                targetH = vw / textureAspect;
            }
            else
            {
                targetH = vh;
                targetW = vh * textureAspect;
            }
            float targetX = vx + (vw - targetW) / 2f;
            float targetY = vy + (vh - targetH) / 2f;
            Rect videoRect = new Rect(targetX, targetY, targetW, targetH);

            MapHelper.DrawBox(new Rect(vx, vy, vw, vh), Color.black);
            GUI.DrawTextureWithTexCoords(videoRect, activeTex, drawCropRect);

            // Draw green crop frame in Editor mode (scaled to the videoRect)
            if (selectedSeqIndex == -1 && isCropEnabled)
            {
                float ox = videoRect.x + cropLeft * videoRect.width;
                float ow = (cropRight - cropLeft) * videoRect.width;
                float oy = videoRect.y + (1.0f - cropTop) * videoRect.height;
                float oh = (cropTop - cropBottom) * videoRect.height;
                MapHelper.DrawBorder(new Rect(ox, oy, ow, oh), Color.green, 2);
            }

            // 2. LEFT OVERLAY PANEL (Width 280) - Edit Controls
            float px = 20;
            float py = 20;
            float pw = 280;
            float ph = Screen.height - 130; // height leaves 110px at bottom

            theme.DrawPanel(new Rect(px, py, pw, ph));

            float cy = py + 15;
            string titleStr = (selectedSeqIndex == -1) ? "EDITOR FULLSCREEN" : "REPLAY SECVENȚĂ";
            GUI.Label(new Rect(px + 15, cy, pw - 30, 22), titleStr, theme.Header);
            cy += 30;
            MapHelper.DrawBox(new Rect(px + 15, cy, pw - 30, 1), new Color(1, 1, 1, 0.15f));
            cy += 15;

            if (selectedSeqIndex == -1)
            {
                // Editor Controls in Fullscreen
                GUI.Label(new Rect(px + 15, cy, pw - 30, 20), "INTERVAL TEMPORAL (TRIM)", theme.Value);
                cy += 25;

                if (GUI.Button(new Rect(px + 15, cy, 120, 24), "Setează Început", theme.Button))
                {
                    startFrameIdx = currentFrameIdx;
                    if (endFrameIdx < startFrameIdx) endFrameIdx = startFrameIdx;
                }

                if (GUI.Button(new Rect(px + 145, cy, 120, 24), "Setează Sfârșit", theme.Button))
                {
                    endFrameIdx = currentFrameIdx;
                    if (startFrameIdx > endFrameIdx) startFrameIdx = endFrameIdx;
                }

                cy += 30;
                if (GUI.Button(new Rect(px + 15, cy, 250, 24), "Resetează Limite", theme.Button))
                {
                    startFrameIdx = 0;
                    endFrameIdx = totalFrames - 1;
                }

                cy += 30;
                GUI.Label(new Rect(px + 15, cy, pw - 30, 18), $"Limite: {FormatTime(startFrameIdx, fps)} - {FormatTime(endFrameIdx, fps)}", theme.Label);
                cy += 25;
                MapHelper.DrawBox(new Rect(px + 15, cy, pw - 30, 1), new Color(1, 1, 1, 0.1f));
                cy += 15;

                // Crop Controls & Auto Record in Fullscreen
                isCropEnabled = GUI.Toggle(new Rect(px + 15, cy, 110, 20), isCropEnabled, "  Crop", theme.Label);
                
                bool autoRecFS = SimulationRecorder.Instance.autoRecordBackground;
                bool newAutoRecFS = GUI.Toggle(new Rect(px + 135, cy, 130, 20), autoRecFS, "  Auto-înreg.", theme.Label);
                if (newAutoRecFS != autoRecFS)
                {
                    SimulationRecorder.Instance.autoRecordBackground = newAutoRecFS;
                }
                cy += 25;

                if (isCropEnabled)
                {
                    GUI.Label(new Rect(px + 15, cy, 70, 16), "Stânga:", theme.Label);
                    cropLeft = GUI.HorizontalSlider(new Rect(px + 90, cy + 4, 175, 16), cropLeft, 0.0f, cropRight - 0.1f);
                    cy += 20;

                    GUI.Label(new Rect(px + 15, cy, 70, 16), "Dreapta:", theme.Label);
                    cropRight = GUI.HorizontalSlider(new Rect(px + 90, cy + 4, 175, 16), cropRight, cropLeft + 0.1f, 1.0f);
                    cy += 20;

                    GUI.Label(new Rect(px + 15, cy, 70, 16), "Jos:", theme.Label);
                    cropBottom = GUI.HorizontalSlider(new Rect(px + 90, cy + 4, 175, 16), cropBottom, 0.0f, cropTop - 0.1f);
                    cy += 20;

                    GUI.Label(new Rect(px + 15, cy, 70, 16), "Sus:", theme.Label);
                    cropTop = GUI.HorizontalSlider(new Rect(px + 90, cy + 4, 175, 16), cropTop, cropBottom + 0.1f, 1.0f);
                    cy += 25;
                }
                else
                {
                    cy += 20;
                }

                MapHelper.DrawBox(new Rect(px + 15, cy, pw - 30, 1), new Color(1, 1, 1, 0.1f));
                cy += 15;

                // Save Area
                GUI.Label(new Rect(px + 15, cy, pw - 30, 20), "SALVARE SECVENȚĂ", theme.Value);
                cy += 25;

                GUI.Label(new Rect(px + 15, cy, 80, 20), "Nume:", theme.Label);
                newSequenceName = GUI.TextField(new Rect(px + 90, cy, 175, 22), newSequenceName, theme.Input);
                cy += 30;

                if (GUI.Button(new Rect(px + 15, cy, 250, 28), "Salvează în Aplicație", theme.Button))
                {
                    Rect finalCropRect = isCropEnabled 
                        ? new Rect(cropLeft, cropBottom, cropRight - cropLeft, cropTop - cropBottom)
                        : new Rect(0f, 0f, 1f, 1f);

                    SimulationRecorder.Instance.SaveSequence(newSequenceName, startFrameIdx, endFrameIdx, finalCropRect);
                    statusMessage = "Salvat cu succes!";
                    statusTimer = Time.unscaledTime + 3f;
                    
                    newSequenceName = $"Secvență_{SimulationRecorder.Instance.savedSequences.Count + 1}";
                }
            }
            else
            {
                // Playback Details & Operations
                var seq = SimulationRecorder.Instance.savedSequences[selectedSeqIndex];
                GUI.Label(new Rect(px + 15, cy, pw - 30, 20), "DETALII FILMĂRE", theme.Value);
                cy += 25;

                GUI.Label(new Rect(px + 15, cy, 60, 20), "Nume:", theme.Label);
                string newName = GUI.TextField(new Rect(px + 80, cy, pw - 95, 20), seq.name, theme.Input);
                if (newName != seq.name)
                {
                    seq.name = newName;
                }
                cy += 25;
                GUI.Label(new Rect(px + 15, cy, pw - 30, 18), $"Dată: {seq.timestamp}", theme.Label);
                cy += 20;
                GUI.Label(new Rect(px + 15, cy, pw - 30, 18), $"Durată: {FormatTime(seq.rawFrames.Count, fps)} ({seq.rawFrames.Count} cadre)", theme.Label);
                cy += 20;
                GUI.Label(new Rect(px + 15, cy, pw - 30, 18), $"Crop: {seq.cropRect.width * 100:F0}% x {seq.cropRect.height * 100:F0}%", theme.Label);
                cy += 35;

                MapHelper.DrawBox(new Rect(px + 15, cy, pw - 30, 1), new Color(1, 1, 1, 0.1f));
                cy += 15;

                if (GUI.Button(new Rect(px + 15, cy, 250, 28), "Exportă JPG pe Disc", theme.Button))
                {
                    string exportPath = SimulationRecorder.Instance.ExportSequenceToDisk(selectedSeqIndex);
                    if (!string.IsNullOrEmpty(exportPath))
                    {
                        statusMessage = "Exportat pe disc!";
                        statusTimer = Time.unscaledTime + 4f;
                    }
                    else
                    {
                        statusMessage = "Eroare la export!";
                        statusTimer = Time.unscaledTime + 4f;
                    }
                }
                cy += 35;

                if (GUI.Button(new Rect(px + 15, cy, 250, 28), "Șterge Filmarea", theme.Button))
                {
                    SimulationRecorder.Instance.StopReplayAudio();
                    SimulationRecorder.Instance.DeleteSequence(selectedSeqIndex);
                    selectedSeqIndex = -1;
                    isPlaying = false;
                    isPlaybackPlaying = false;
                    isFullscreenMode = false; // exit fullscreen since we deleted it
                    statusMessage = "Șters.";
                    statusTimer = Time.unscaledTime + 3f;
                    return;
                }
            }

            // Draw status feedback
            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUI.Label(new Rect(px + 15, cy + 20, 250, 20), statusMessage, theme.Good);
            }

            // Close button at the bottom of the left sidepanel
            if (GUI.Button(new Rect(px + 15, py + ph - 40, pw - 30, 28), "✖ ÎNCHIDE ECRAN MARE", theme.Button))
            {
                isFullscreenMode = false;
                SimulationRecorder.Instance.StopReplayAudio();
            }

            // 3. BOTTOM SCRUBBER OVERLAY PANEL
            float sx = 20;
            float sy = Screen.height - 90;
            float sw = Screen.width - 40;
            float sh = 70;

            Rect scrubberRect = new Rect(sx, sy, sw, sh);
            theme.DrawPanel(scrubberRect);

            // Play/Pause button
            bool isCurrentPlaying = (selectedSeqIndex == -1) ? isPlaying : isPlaybackPlaying;
            if (GUI.Button(new Rect(sx + 20, sy + 23, 110, 24), isCurrentPlaying ? "❚❚ PAUZĂ" : "▶ REDARE", theme.Button))
            {
                if (selectedSeqIndex == -1)
                {
                    isPlaying = !isPlaying;
                    if (isPlaying)
                    {
                        if (currentFrameIdx < startFrameIdx || currentFrameIdx > endFrameIdx)
                            currentFrameIdx = startFrameIdx;
                        
                        float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                        playbackStartTime = Time.unscaledTime - (currentFrameIdx - startFrameIdx) * timePerFrame;
                    }
                }
                else
                {
                    isPlaybackPlaying = !isPlaybackPlaying;
                    if (isPlaybackPlaying)
                    {
                        float timePerFrame = 1f / SimulationRecorder.Instance.captureFPS;
                        playbackStartTime = Time.unscaledTime - playbackFrameIdx * timePerFrame;
                        SimulationRecorder.Instance.PlayReplayAudio(selectedSeqIndex, playbackFrameIdx);
                    }
                    else
                    {
                        SimulationRecorder.Instance.PauseReplayAudio();
                    }
                }
            }

            // Timeline slider
            float sliderWidth = sw - 330;
            GUI.Label(new Rect(sx + 150, sy + 27, 40, 16), "Timp:", theme.Label);

            int newFrameIdx;
            if (selectedSeqIndex == -1)
            {
                newFrameIdx = (int)GUI.HorizontalSlider(new Rect(sx + 195, sy + 31, sliderWidth, 16), currentFrameIdx, 0, totalFrames - 1);
                if (newFrameIdx != currentFrameIdx)
                {
                    currentFrameIdx = newFrameIdx;
                    isPlaying = false;
                }
                string timeStr = $"{FormatTime(currentFrameIdx, fps)} / {FormatTime(totalFrames - 1, fps)}";
                GUI.Label(new Rect(sx + 210 + sliderWidth, sy + 27, 110, 16), timeStr, theme.Value);
            }
            else
            {
                newFrameIdx = (int)GUI.HorizontalSlider(new Rect(sx + 195, sy + 31, sliderWidth, 16), playbackFrameIdx, 0, totalFrames - 1);
                if (newFrameIdx != playbackFrameIdx)
                {
                    playbackFrameIdx = newFrameIdx;
                    isPlaybackPlaying = false;
                    SimulationRecorder.Instance.StopReplayAudio();
                }
                string timeStr = $"{FormatTime(playbackFrameIdx, fps)} / {FormatTime(totalFrames - 1, fps)}";
                GUI.Label(new Rect(sx + 210 + sliderWidth, sy + 27, 110, 16), timeStr, theme.Value);
            }
        }

        private string FormatTime(int frameIdx, int fps)
        {
            if (fps <= 0) fps = 10;
            float totalSeconds = (float)frameIdx / fps;
            int minutes = Mathf.FloorToInt(totalSeconds / 60f);
            int seconds = Mathf.FloorToInt(totalSeconds % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
