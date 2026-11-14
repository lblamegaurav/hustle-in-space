using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SpaceSim.Core;

namespace SpaceSim.UI
{
    /// <summary>
    /// Handles the authentic aerospace boot loading screen and the first-time player tutorial modal.
    /// Simulates telemetry subsystem boot-up with audio beeps and progress tracking.
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

        // Splash UI Elements
        private RectTransform _orbitLogoTransform;
        private Image _progressBarFill;
        private Text _progressPercentText;
        private Text _bootLogText;

        // Tutorial UI Elements
        private Button _dismissTutorialBtn;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildLoadingAndTutorialUI();
            StartCoroutine(RunBootSequence());
        }

        private void Update()
        {
            // Spin the animated orbit logo in the loading screen
            if (_orbitLogoTransform != null && splashPanel.activeSelf)
            {
                _orbitLogoTransform.Rotate(0, 0, -45f * Time.deltaTime);
            }
        }

        private IEnumerator RunBootSequence()
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

            float duration = 3.2f;
            float elapsed = 0f;
            int lastMsgIndex = -1;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                if (_progressBarFill != null) _progressBarFill.fillAmount = progress;
                if (_progressPercentText != null) _progressPercentText.text = $"{Mathf.RoundToInt(progress * 100f):000}%";

                int msgIndex = Mathf.Clamp(Mathf.FloorToInt(progress * bootMessages.Length), 0, bootMessages.Length - 1);
                if (msgIndex != lastMsgIndex)
                {
                    lastMsgIndex = msgIndex;
                    if (_bootLogText != null) _bootLogText.text = $"> {bootMessages[msgIndex]}";
                    SpaceSimAudio.Instance.PlayBeep(880f + msgIndex * 120f, 0.04f, 0.25f);
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

            // Center Content Container
            var centerGo = new GameObject("CenterContainer");
            centerGo.transform.SetParent(splashPanel.transform, false);
            var centerRt = centerGo.AddComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.5f, 0.5f);
            centerRt.anchorMax = new Vector2(0.5f, 0.5f);
            centerRt.sizeDelta = new Vector2(650, 420);
            centerRt.anchoredPosition = Vector2.zero;

            // Animated Orbit Ring Logo
            var logoGo = new GameObject("OrbitLogo");
            logoGo.transform.SetParent(centerGo.transform, false);
            _orbitLogoTransform = logoGo.AddComponent<RectTransform>();
            _orbitLogoTransform.sizeDelta = new Vector2(100, 100);
            _orbitLogoTransform.anchoredPosition = new Vector2(0, 110);

            var logoOutline = logoGo.AddComponent<Outline>();
            logoOutline.effectColor = new Color(0f, 0.85f, 1f, 0.8f);
            logoOutline.effectDistance = new Vector2(2, 2);

            var ringImg = logoGo.AddComponent<Image>();
            ringImg.color = new Color(0.05f, 0.25f, 0.45f, 0.5f);

            // Orbiting Dot
            var dotGo = new GameObject("OrbitDot");
            dotGo.transform.SetParent(logoGo.transform, false);
            var dotRt = dotGo.AddComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(14, 14);
            dotRt.anchoredPosition = new Vector2(44, 0);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = new Color(0.1f, 1f, 0.8f);

            // Simulation Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(centerGo.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "NASA AEROSPACE SIMULATION SYSTEMS";

            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(600, 36);
            titleRt.anchoredPosition = new Vector2(0, 30);

            // Subtitle
            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(centerGo.transform, false);
            var subText = subGo.AddComponent<Text>();
            subText.font = font;
            subText.fontSize = 13;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.5f, 0.75f, 0.9f);
            subText.text = "APOLLO 11 SATURN V  &  STS-107 SPACE SHUTTLE COLUMBIA";

            var subRt = subGo.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(600, 24);
            subRt.anchoredPosition = new Vector2(0, 0);

            // Progress Bar Background
            var barBgGo = new GameObject("ProgressBarBg");
            barBgGo.transform.SetParent(centerGo.transform, false);
            var barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.sizeDelta = new Vector2(500, 10);
            barBgRt.anchoredPosition = new Vector2(0, -60);

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
            pctGo.transform.SetParent(centerGo.transform, false);
            _progressPercentText = pctGo.AddComponent<Text>();
            _progressPercentText.font = font;
            _progressPercentText.fontSize = 13;
            _progressPercentText.fontStyle = FontStyle.Bold;
            _progressPercentText.alignment = TextAnchor.MiddleCenter;
            _progressPercentText.color = new Color(0.0f, 0.9f, 1.0f);
            _progressPercentText.text = "000%";

            var pctRt = pctGo.GetComponent<RectTransform>();
            pctRt.sizeDelta = new Vector2(100, 24);
            pctRt.anchoredPosition = new Vector2(0, -82);

            // Telemetry Log Text
            var logGo = new GameObject("BootLogText");
            logGo.transform.SetParent(centerGo.transform, false);
            _bootLogText = logGo.AddComponent<Text>();
            _bootLogText.font = font;
            _bootLogText.fontSize = 12;
            _bootLogText.alignment = TextAnchor.MiddleCenter;
            _bootLogText.color = new Color(0.7f, 0.8f, 0.85f);
            _bootLogText.text = "> INITIALIZING FLIGHT SYSTEMS...";

            var logRt = logGo.GetComponent<RectTransform>();
            logRt.sizeDelta = new Vector2(600, 24);
            logRt.anchoredPosition = new Vector2(0, -115);
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
            bg.color = new Color(0f, 0f, 0f, 0.85f); // Dimmed backdrop

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
