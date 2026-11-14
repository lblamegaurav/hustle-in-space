using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using SpaceSim.Core;

namespace SpaceSim.UI
{
    /// <summary>
    /// Handles the authentic aerospace boot loading screen and the first-time player tutorial modal.
    /// Plays gameloading.mp4 full-screen with real-time telemetry overlays and audio.
    /// Manages PlayerPrefs persistence for onboarding so returning players jump straight to the mission palette.
    /// </summary>
    public class GameLoadingManager : MonoBehaviour
    {
        public static GameLoadingManager Instance { get; private set; }

        [Header("Persistence Key")]
        public const string TutorialSeenPrefKey = "SPACE_SIM_TUTORIAL_SEEN";

        [Header("UI Canvas & Screens")]
        public Canvas loadingCanvas;
        public GameObject splashPanel;
        public GameObject tutorialModal;

        [Header("Video Player Elements")]
        public VideoPlayer videoPlayer;
        public RawImage videoDisplay;
        private RenderTexture _videoRenderTexture;

        // Splash UI Elements
        private RectTransform _orbitLogoTransform;
        private Image _progressBarFill;
        private Text _progressPercentText;
        private Text _bootLogText;
        private Button _skipButton;

        private bool _isSkipped = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildLoadingAndTutorialUI();
            StartCoroutine(RunBootSequenceWithVideo());
        }

        private void Update()
        {
            // Spin the backup animated orbit logo if active
            if (_orbitLogoTransform != null && _orbitLogoTransform.gameObject.activeSelf)
            {
                _orbitLogoTransform.Rotate(0, 0, -45f * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_videoRenderTexture != null)
            {
                _videoRenderTexture.Release();
                Destroy(_videoRenderTexture);
            }
        }

        private IEnumerator RunBootSequenceWithVideo()
        {
            if (splashPanel != null) splashPanel.SetActive(true);
            if (tutorialModal != null) tutorialModal.SetActive(false);

            string[] bootMessages = new string[]
            {
                "INITIALIZING GUIDANCE & LVDC NAVIGATION PLATFORM...",
                "COMPILING URP AEROSPACE SHADERS & 3D MESH ENGINES...",
                "CALIBRATING CRYOGENIC PROPELLANT PRESSURE SENSORS...",
                "MOUNTING APOLLO 11 & STS-107 MISSION DATA ARCHIVES...",
                "ESTABLISHING HIGH-SPEED TELEMETRY LINK TO KSC LAUNCH CONTROL...",
                "ALL FLIGHT SYSTEMS NOMINAL — BOOT COMPLETE."
            };

            // Locate gameloading.mp4 in StreamingAssets or Project root
            string videoPath = Path.Combine(Application.streamingAssetsPath, "gameloading.mp4");
            if (!File.Exists(videoPath))
            {
                // Fallback to project root or assets
                videoPath = Path.Combine(Application.dataPath, "Videos/gameloading.mp4");
                if (!File.Exists(videoPath))
                {
                    videoPath = Path.Combine(Directory.GetCurrentDirectory(), "gameloading.mp4");
                }
            }

            bool hasVideo = File.Exists(videoPath) && videoPlayer != null;

            if (hasVideo)
            {
                Debug.Log($"<color=cyan><b>[GameLoadingManager]</b></color> Loading video background from: {videoPath}");
                videoPlayer.url = videoPath;
                videoPlayer.Prepare();

                while (!videoPlayer.isPrepared && !_isSkipped)
                {
                    yield return null;
                }

                if (!_isSkipped)
                {
                    videoPlayer.Play();
                    if (_orbitLogoTransform != null) _orbitLogoTransform.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("<color=yellow><b>[GameLoadingManager]</b></color> Video file not found, falling back to vector boot animation.");
                if (_orbitLogoTransform != null) _orbitLogoTransform.gameObject.SetActive(true);
            }

            float videoDuration = (hasVideo && videoPlayer.length > 0) ? (float)videoPlayer.length : 10.0f;
            float elapsed = 0f;
            int lastMsgIndex = -1;

            while (elapsed < videoDuration && !_isSkipped)
            {
                elapsed += Time.deltaTime;

                float progress = 0f;
                if (hasVideo && videoPlayer.isPlaying && videoPlayer.length > 0)
                {
                    progress = Mathf.Clamp01((float)(videoPlayer.time / videoPlayer.length));
                }
                else
                {
                    progress = Mathf.Clamp01(elapsed / videoDuration);
                }

                if (_progressBarFill != null) _progressBarFill.fillAmount = progress;
                if (_progressPercentText != null) _progressPercentText.text = $"{Mathf.RoundToInt(progress * 100f):000}%";

                int msgIndex = Mathf.Clamp(Mathf.FloorToInt(progress * bootMessages.Length), 0, bootMessages.Length - 1);
                if (msgIndex != lastMsgIndex)
                {
                    lastMsgIndex = msgIndex;
                    if (_bootLogText != null) _bootLogText.text = $"> {bootMessages[msgIndex]}";
                    SpaceSimAudio.Instance.PlayBeep(880f + msgIndex * 120f, 0.04f, 0.2f);
                }

                yield return null;
            }

            if (_progressBarFill != null) _progressBarFill.fillAmount = 1f;
            if (_progressPercentText != null) _progressPercentText.text = "100%";
            SpaceSimAudio.Instance.PlayVerifiedChime();

            yield return new WaitForSeconds(0.4f);

            // Check if player has seen the tutorial
            bool hasSeenTutorial = PlayerPrefs.GetInt(TutorialSeenPrefKey, 0) == 1;

            if (!hasSeenTutorial)
            {
                // First-time player: Show onboarding tutorial modal
                if (splashPanel != null) splashPanel.SetActive(false);
                OpenTutorialModal();
            }
            else
            {
                // Returning player: Fade out splash directly into mission selection
                yield return StartCoroutine(FadeOutSplash());
                if (MissionPaletteManager.Instance != null)
                {
                    MissionPaletteManager.Instance.ShowMissionSelect();
                }
            }
        }

        public void SkipLoading()
        {
            _isSkipped = true;
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
        }

        private IEnumerator FadeOutSplash()
        {
            var cg = splashPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = splashPanel.AddComponent<CanvasGroup>();

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - (t / 0.5f);
                yield return null;
            }

            splashPanel.SetActive(false);
        }

        // ==========================================
        // TUTORIAL MODAL
        // ==========================================
        public void OpenTutorialModal()
        {
            if (tutorialModal != null)
            {
                tutorialModal.SetActive(true);
                SpaceSimAudio.Instance.PlaySelect();
            }
        }

        public void DismissTutorial()
        {
            // Record that user has seen tutorial so it does not auto-open on future launches
            PlayerPrefs.SetInt(TutorialSeenPrefKey, 1);
            PlayerPrefs.Save();

            SpaceSimAudio.Instance.PlayClick();

            if (tutorialModal != null) tutorialModal.SetActive(false);
            if (splashPanel != null && splashPanel.activeSelf) splashPanel.SetActive(false);

            if (MissionPaletteManager.Instance != null)
            {
                MissionPaletteManager.Instance.ShowMissionSelect();
            }
        }

        // ==========================================
        // DYNAMIC UI CREATION (UGUI)
        // ==========================================
        private void BuildLoadingAndTutorialUI()
        {
            if (loadingCanvas == null)
            {
                var canvasGo = GameObject.Find("SpaceSim_MainCanvas");
                if (canvasGo != null)
                {
                    loadingCanvas = canvasGo.GetComponent<Canvas>();
                }
                else
                {
                    canvasGo = new GameObject("SpaceSim_MainCanvas");
                    loadingCanvas = canvasGo.AddComponent<Canvas>();
                    loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var cs = canvasGo.AddComponent<CanvasScaler>();
                    cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    cs.referenceResolution = new Vector2(1920, 1080);
                    canvasGo.AddComponent<GraphicRaycaster>();
                }
            }

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Splash Screen
            BuildSplashPanel(loadingCanvas.transform, font);

            // 2. Tutorial Modal
            BuildTutorialModal(loadingCanvas.transform, font);
        }

        private void BuildSplashPanel(Transform parent, Font font)
        {
            splashPanel = new GameObject("SplashLoadingPanel");
            splashPanel.transform.SetParent(parent, false);

            var rt = splashPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = splashPanel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.06f, 1f);

            var cg = splashPanel.AddComponent<CanvasGroup>();

            // Video Player & RawImage Display (Full Screen Background)
            var videoGo = new GameObject("VideoBackground");
            videoGo.transform.SetParent(splashPanel.transform, false);
            var videoRt = videoGo.AddComponent<RectTransform>();
            videoRt.anchorMin = Vector2.zero;
            videoRt.anchorMax = Vector2.one;
            videoRt.offsetMin = Vector2.zero;
            videoRt.offsetMax = Vector2.zero;

            videoDisplay = videoGo.AddComponent<RawImage>();
            videoDisplay.color = Color.white;

            // Setup RenderTexture for Video
            _videoRenderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
            _videoRenderTexture.Create();
            videoDisplay.texture = _videoRenderTexture;

            // Video Player component
            videoPlayer = videoGo.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = _videoRenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            var audioSource = videoGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            // Skip Button in Top-Right
            var skipGo = new GameObject("SkipBtn");
            skipGo.transform.SetParent(splashPanel.transform, false);
            var skipRt = skipGo.AddComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1, 1);
            skipRt.anchorMax = new Vector2(1, 1);
            skipRt.pivot = new Vector2(1, 1);
            skipRt.sizeDelta = new Vector2(110, 36);
            skipRt.anchoredPosition = new Vector2(-25, -25);

            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            var skipOutline = skipGo.AddComponent<Outline>();
            skipOutline.effectColor = new Color(0.2f, 0.5f, 0.8f, 0.6f);

            _skipButton = skipGo.AddComponent<Button>();
            _skipButton.onClick.AddListener(SkipLoading);

            var skipTxtGo = new GameObject("Label");
            skipTxtGo.transform.SetParent(skipGo.transform, false);
            var skipTxt = skipTxtGo.AddComponent<Text>();
            skipTxt.font = font;
            skipTxt.fontSize = 12;
            skipTxt.fontStyle = FontStyle.Bold;
            skipTxt.alignment = TextAnchor.MiddleCenter;
            skipTxt.color = new Color(0.8f, 0.9f, 1.0f);
            skipTxt.text = "SKIP ⏭";
            var skipTxtRt = skipTxtGo.GetComponent<RectTransform>();
            skipTxtRt.anchorMin = Vector2.zero;
            skipTxtRt.anchorMax = Vector2.one;
            skipTxtRt.sizeDelta = Vector2.zero;

            // Telemetry & Progress Overlay (Bottom Dock of Loading Screen)
            var bottomDockGo = new GameObject("BottomTelemetryDock");
            bottomDockGo.transform.SetParent(splashPanel.transform, false);
            var bottomRt = bottomDockGo.AddComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0);
            bottomRt.pivot = new Vector2(0.5f, 0);
            bottomRt.sizeDelta = new Vector2(0, 140);

            var bottomBg = bottomDockGo.AddComponent<Image>();
            bottomBg.color = new Color(0.02f, 0.04f, 0.06f, 0.82f); // Glassmorphism backdrop

            // Progress Bar Background
            var barBgGo = new GameObject("ProgressBarBg");
            barBgGo.transform.SetParent(bottomDockGo.transform, false);
            var barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRt.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRt.sizeDelta = new Vector2(700, 10);
            barBgRt.anchoredPosition = new Vector2(0, 15);

            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.12f, 0.16f, 0.22f);

            // Progress Bar Fill
            var barFillGo = new GameObject("ProgressBarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRt = barFillGo.AddComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.sizeDelta = Vector2.zero;

            _progressBarFill = barFillGo.AddComponent<Image>();
            _progressBarFill.color = new Color(0.0f, 0.9f, 1.0f);
            _progressBarFill.type = Image.Type.Filled;
            _progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            _progressBarFill.fillAmount = 0f;

            // Percent Text
            var pctGo = new GameObject("PercentText");
            pctGo.transform.SetParent(bottomDockGo.transform, false);
            _progressPercentText = pctGo.AddComponent<Text>();
            _progressPercentText.font = font;
            _progressPercentText.fontSize = 13;
            _progressPercentText.fontStyle = FontStyle.Bold;
            _progressPercentText.alignment = TextAnchor.MiddleCenter;
            _progressPercentText.color = new Color(0.0f, 0.9f, 1.0f);
            _progressPercentText.text = "000%";

            var pctRt = pctGo.GetComponent<RectTransform>();
            pctRt.anchorMin = new Vector2(0.5f, 0.5f);
            pctRt.anchorMax = new Vector2(0.5f, 0.5f);
            pctRt.sizeDelta = new Vector2(100, 24);
            pctRt.anchoredPosition = new Vector2(0, -10);

            // Telemetry Log Text
            var logGo = new GameObject("BootLogText");
            logGo.transform.SetParent(bottomDockGo.transform, false);
            _bootLogText = logGo.AddComponent<Text>();
            _bootLogText.font = font;
            _bootLogText.fontSize = 12;
            _bootLogText.alignment = TextAnchor.MiddleCenter;
            _bootLogText.color = new Color(0.7f, 0.82f, 0.92f);
            _bootLogText.text = "> INITIALIZING FLIGHT SYSTEMS...";

            var logRt = logGo.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0.5f, 0.5f);
            logRt.anchorMax = new Vector2(0.5f, 0.5f);
            logRt.sizeDelta = new Vector2(800, 24);
            logRt.anchoredPosition = new Vector2(0, -35);
        }

        private void BuildTutorialModal(Transform parent, Font font)
        {
            tutorialModal = new GameObject("TutorialModalPanel");
            tutorialModal.transform.SetParent(parent, false);

            var rt = tutorialModal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = tutorialModal.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.88f); // Dimmed backdrop

            // Modal Card Box
            var cardGo = new GameObject("ModalCard");
            cardGo.transform.SetParent(tutorialModal.transform, false);
            var cardRt = cardGo.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(740, 560);
            cardRt.anchoredPosition = Vector2.zero;

            var cardBg = cardGo.AddComponent<Image>();
            cardBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);

            var outline = cardGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.85f, 1.0f, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(32, 32, 28, 28);
            vlg.spacing = 16f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Modal Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(cardGo.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.0f, 0.9f, 1.0f);
            titleText.text = "FLIGHT OPERATIONS MANUAL & SIMULATOR BRIEFING";

            // Body Description & Instructions
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(cardGo.transform, false);
            var bodyText = bodyGo.AddComponent<Text>();
            bodyText.font = font;
            bodyText.fontSize = 13;
            bodyText.lineSpacing = 1.35f;
            bodyText.color = new Color(0.88f, 0.92f, 0.96f);
            bodyText.text =
                "Welcome, Flight Director. This simulation delivers authentic aerospace telemetry and 3D flight mechanics for humanity's monumental space expeditions.\n\n" +
                "<b>1. MISSION SELECTION</b>\n" +
                "Choose between <b>Apollo 11 (Saturn V Lunar Expedition)</b> and <b>STS-107 (Space Shuttle Columbia Tribute)</b>. Only hardware belonging to your active mission will be rendered.\n\n" +
                "<b>2. 3D BLENDER-STYLE VIEWPORT CONTROLS</b>\n" +
                "• <b>Orbit / Rotate:</b> Click and drag with the <i>Left Mouse Button</i>.\n" +
                "• <b>Zoom:</b> Use the <i>Mouse Scroll Wheel</i> to inspect fine rocket details.\n" +
                "• <b>Pan:</b> Drag with the <i>Middle Mouse Button</i> or <i>Right Mouse Button</i>.\n\n" +
                "<b>3. HARDWARE INSPECTION & VERIFICATION DOCK</b>\n" +
                "Click each stage on the right-hand hierarchy dock. Examine its manufacturer, mass metrics, propulsion chemistry (RP-1/LOX vs LH2/LOX), and peak hazards.\n\n" +
                "<b>4. ROLLOUT & LAUNCH CLEARANCE</b>\n" +
                "All rocket stages must be thoroughly inspected to unlock launch authorization. Click <i>Authorize Rollout</i> once all systems are verified.";

            var bodyLe = bodyGo.AddComponent<LayoutElement>();
            bodyLe.minHeight = 360f;

            // Dismiss Button
            var btnGo = new GameObject("DismissBtn");
            btnGo.transform.SetParent(cardGo.transform, false);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.minHeight = 44f;

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.0f, 0.55f, 0.85f);

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.0f, 0.55f, 0.85f);
            colors.highlightedColor = new Color(0.1f, 0.75f, 1f);
            colors.pressedColor = new Color(0.0f, 0.4f, 0.65f);
            btn.colors = colors;

            var btnTextGo = new GameObject("Label");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = font;
            btnText.fontSize = 14;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "UNDERSTOOD — COMMENCE SIMULATION ▸";

            var btnTextRt = btnTextGo.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(DismissTutorial);
        }
    }
}
