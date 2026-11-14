using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SpaceSim.Core;
using SpaceSim.Data;
using SpaceSim.Models;

namespace SpaceSim.UI
{
    /// <summary>
    /// Master controller for the Mission Selection and Blender-like 3D Assembly & Inspection Palette.
    /// Filters 3D assets and technical documentation according to the chosen mission (Saturn V or Space Shuttle).
    /// Tracks module inspection, manages 3D stage highlighting and camera framing, and gates rollout authorization.
    /// </summary>
    public class MissionPaletteManager : MonoBehaviour
    {
        public static MissionPaletteManager Instance { get; private set; }

        [Header("3D Scene References")]
        public GameObject saturnVRoot;
        public GameObject spaceShuttleRoot;
        public BlenderOrbitCamera orbitCamera;

        [Header("Active Mission State")]
        public string currentMissionId = "SaturnV";
        private MissionDefinition _activeMission;
        private HashSet<string> _verifiedModules = new HashSet<string>();
        private RocketModuleInfo _selectedModule = null;

        [Header("UI Canvas & Panels")]
        public Canvas mainCanvas;
        public GameObject missionSelectPanel;
        public GameObject blenderPalettePanel;
        public GameObject tutorialModalPanel;

        // UI Components - Mission Selection
        private Button _saturnSelectBtn;
        private Button _shuttleSelectBtn;

        // UI Components - Blender Dock
        private Text _dockTitleText;
        private Text _dockProgressText;
        private Transform _moduleListContainer;
        private Text _detailsTitleText;
        private Text _detailsSubtitleText;
        private Text _detailsBodyText;
        private Button _rolloutButton;
        private Text _rolloutButtonText;
        private Button _backToMissionsBtn;

        // Visual Highlight Cache
        private Dictionary<string, GameObject> _cachedStageObjects = new Dictionary<string, GameObject>();
        private Coroutine _pulseCoroutine;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Auto-find or build 3D rocket objects if not assigned
            Ensure3DModelsExist();

            // Auto-setup camera
            if (orbitCamera == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    orbitCamera = mainCam.GetComponent<BlenderOrbitCamera>();
                    if (orbitCamera == null) orbitCamera = mainCam.gameObject.AddComponent<BlenderOrbitCamera>();
                }
            }

            // Build dynamic UGUI hierarchy if canvas doesn't have child panels assigned
            BuildDynamicUI();

            // Default view: Show Mission Selection Screen first
            ShowMissionSelect();
        }

        private void Ensure3DModelsExist()
        {
            if (saturnVRoot == null)
            {
                saturnVRoot = GameObject.Find("SaturnV_Apollo11");
                if (saturnVRoot == null)
                {
                    // Build Saturn V procedurally via SaturnVBuilder
                    saturnVRoot = SaturnV.SaturnVBuilder.BuildCompleteRocket();
                }
            }

            if (spaceShuttleRoot == null)
            {
                spaceShuttleRoot = GameObject.Find("SpaceShuttle_Stack");
                if (spaceShuttleRoot == null)
                {
                    // Build Space Shuttle procedurally via SpaceShuttleBuilder
                    spaceShuttleRoot = SpaceShuttleBuilder.BuildCompleteShuttle();
                }
            }

            // Cache stage GameObjects for fast lookup & focus
            CacheStageObjects(saturnVRoot);
            CacheStageObjects(spaceShuttleRoot);
        }

        private void CacheStageObjects(GameObject root)
        {
            if (root == null) return;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (!_cachedStageObjects.ContainsKey(t.name))
                {
                    _cachedStageObjects[t.name] = t.gameObject;
                }
            }
        }

        // ==========================================
        // MISSION SELECTION LOGIC
        // ==========================================
        public void ShowMissionSelect()
        {
            if (missionSelectPanel != null) missionSelectPanel.SetActive(true);
            if (blenderPalettePanel != null) blenderPalettePanel.SetActive(false);

            // Hide both rockets during top-level selection
            if (saturnVRoot != null) saturnVRoot.SetActive(false);
            if (spaceShuttleRoot != null) spaceShuttleRoot.SetActive(false);

            SpaceSimAudio.Instance.PlaySelect();
        }

        public void SelectMission(string missionId)
        {
            currentMissionId = missionId;
            _activeMission = MissionDatabase.GetMission(missionId);
            _verifiedModules.Clear();
            _selectedModule = null;

            SpaceSimAudio.Instance.PlayVerifiedChime();

            // 1. Dynamic filtering of 3D Models
            if (currentMissionId == "SaturnV")
            {
                if (saturnVRoot != null) saturnVRoot.SetActive(true);
                if (spaceShuttleRoot != null) spaceShuttleRoot.SetActive(false);
                if (orbitCamera != null)
                {
                    orbitCamera.pivotPoint = new Vector3(0, 50f, 0);
                    orbitCamera.distance = 75f;
                }
            }
            else // Space Shuttle
            {
                if (saturnVRoot != null) saturnVRoot.SetActive(false);
                if (spaceShuttleRoot != null) spaceShuttleRoot.SetActive(true);
                if (orbitCamera != null)
                {
                    orbitCamera.pivotPoint = new Vector3(0, 24f, 0);
                    orbitCamera.distance = 55f;
                }
            }

            // 2. Open Blender Palette
            if (missionSelectPanel != null) missionSelectPanel.SetActive(false);
            if (blenderPalettePanel != null) blenderPalettePanel.SetActive(true);

            // 3. Populate Right Dock with this mission's modules
            PopulateDockForMission();
        }

        // ==========================================
        // BLENDER-LIKE PALETTE & DOCK
        // ==========================================
        private void PopulateDockForMission()
        {
            if (_activeMission == null) return;

            if (_dockTitleText != null)
            {
                _dockTitleText.text = $"<b>{_activeMission.missionCode} — VEHICLE DOCK</b>\n<size=11><color=#88A0B8>{_activeMission.vehicleName}</color></size>";
            }

            UpdateVerificationProgress();

            // Clear old module rows
            if (_moduleListContainer != null)
            {
                foreach (Transform child in _moduleListContainer)
                {
                    Destroy(child.gameObject);
                }

                // Create a row button for each module
                for (int i = 0; i < _activeMission.modules.Count; i++)
                {
                    var mod = _activeMission.modules[i];
                    CreateModuleListRow(mod, i);
                }
            }

            // Select the first module by default
            if (_activeMission.modules.Count > 0)
            {
                SelectModule(_activeMission.modules[0]);
            }
        }

        private void CreateModuleListRow(RocketModuleInfo mod, int index)
        {
            var rowGo = new GameObject("ModuleRow_" + mod.id);
            rowGo.transform.SetParent(_moduleListContainer, false);

            var le = rowGo.AddComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 44f;
            le.flexibleWidth = 1f;

            var img = rowGo.AddComponent<Image>();
            img.color = new Color(0.10f, 0.13f, 0.18f, 0.85f);

            var btn = rowGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.10f, 0.13f, 0.18f, 0.85f);
            colors.highlightedColor = new Color(0.18f, 0.28f, 0.40f, 0.95f);
            colors.pressedColor = new Color(0.05f, 0.45f, 0.65f, 1f);
            btn.colors = colors;

            // Row Content Horizontal Layout
            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 6, 6);
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;

            // Checkmark Icon
            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(rowGo.transform, false);
            var checkText = checkGo.AddComponent<Text>();
            checkText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            checkText.fontSize = 14;
            checkText.alignment = TextAnchor.MiddleCenter;
            checkText.color = _verifiedModules.Contains(mod.id) ? new Color(0.2f, 1f, 0.6f) : new Color(0.5f, 0.5f, 0.5f);
            checkText.text = _verifiedModules.Contains(mod.id) ? "✓" : "○";
            var checkLe = checkGo.AddComponent<LayoutElement>();
            checkLe.preferredWidth = 20f;

            // Module Label Text
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 13;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.text = $"<b>{mod.displayName}</b>\n<size=10><color=#90A4AE>{mod.subtitle}</color></size>";
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;

            btn.onClick.AddListener(() =>
            {
                SelectModule(mod);
                // Update checkmark
                checkText.text = "✓";
                checkText.color = new Color(0.2f, 1f, 0.6f);
            });
        }

        public void SelectModule(RocketModuleInfo mod)
        {
            _selectedModule = mod;
            _verifiedModules.Add(mod.id);

            SpaceSimAudio.Instance.PlaySelect();

            // 1. Update Details Panel Text
            if (_detailsTitleText != null) _detailsTitleText.text = $"<b>{mod.displayName.ToUpper()}</b>";
            if (_detailsSubtitleText != null) _detailsSubtitleText.text = $"<color=#00E5FF>MANUFACTURER:</color> {mod.manufacturer}\n<color=#00E5FF>DIMENSIONS:</color> {mod.dimensions}";

            if (_detailsBodyText != null)
            {
                _detailsBodyText.text =
                    $"<b><color=#80D8FF>▸ MASS & METRICS:</color></b>\n{mod.massSpecs}\n\n" +
                    $"<b><color=#80D8FF>▸ PROPULSION & THERMODYNAMICS:</color></b>\n{mod.propulsionChemistry}\n{mod.thrustAndIsp}\n\n" +
                    $"<b><color=#80D8FF>▸ MATERIALS & METALLURGY:</color></b>\n{mod.structuralMaterials}\n\n" +
                    $"<b><color=#FF8A80>▸ TECHNICAL HAZARDS & VULNERABILITIES:</color></b>\n{mod.technicalHazards}\n\n" +
                    $"<b><color=#B9F6CA>▸ HISTORICAL FLIGHT LEGACY:</color></b>\n{mod.historicalSignificance}";
            }

            // 2. Camera Framing in 3D Viewport
            if (orbitCamera != null)
            {
                Vector3 focusPivot = new Vector3(0, mod.cameraFocusY, 0);
                orbitCamera.FocusOn(focusPivot, mod.cameraDistance);
            }

            // 3. Highlight 3D Stage Mesh
            HighlightStageObject(mod.gameObjectName);

            // 4. Update Verification Progress & Gate
            UpdateVerificationProgress();
        }

        private void HighlightStageObject(string goName)
        {
            if (string.IsNullOrEmpty(goName)) return;

            if (_cachedStageObjects.TryGetValue(goName, out GameObject targetGo))
            {
                if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = StartCoroutine(PulseStageHighlight(targetGo));
            }
        }

        private IEnumerator PulseStageHighlight(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) yield break;

            var originalColors = new Dictionary<Material, Color>();
            foreach (var r in renderers)
            {
                foreach (var m in r.materials)
                {
                    if (!originalColors.ContainsKey(m) && m.HasProperty("_Color"))
                    {
                        originalColors[m] = m.color;
                    }
                }
            }

            // Flash effect for 1.2s
            float elapsed = 0f;
            float duration = 1.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.Sin(elapsed * Mathf.PI * 4f) * 0.5f + 0.5f;

                foreach (var kvp in originalColors)
                {
                    var m = kvp.Key;
                    if (m != null && m.HasProperty("_Color"))
                    {
                        m.color = Color.Lerp(kvp.Value, Color.cyan, pulse * 0.6f);
                    }
                }
                yield return null;
            }

            // Restore original colors
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null && kvp.Key.HasProperty("_Color"))
                {
                    kvp.Key.color = kvp.Value;
                }
            }
        }

        private void UpdateVerificationProgress()
        {
            if (_activeMission == null) return;

            int verified = _verifiedModules.Count;
            int total = _activeMission.modules.Count;

            if (_dockProgressText != null)
            {
                _dockProgressText.text = $"SYSTEMS VERIFIED: <b>{verified} / {total}</b>";
            }

            bool allVerified = verified >= total;

            if (_rolloutButton != null)
            {
                _rolloutButton.interactable = allVerified;
            }

            if (_rolloutButtonText != null)
            {
                if (allVerified)
                {
                    _rolloutButtonText.text = "<b>AUTHORIZE ROLLOUT & COMMENCE MISSION ▸</b>";
                    _rolloutButtonText.color = new Color(0.1f, 1f, 0.7f);
                }
                else
                {
                    _rolloutButtonText.text = $"INSPECT ALL MODULES TO AUTHORIZE ({verified}/{total})";
                    _rolloutButtonText.color = new Color(0.6f, 0.65f, 0.7f);
                }
            }
        }

        // ==========================================
        // LAUNCH & HANDOFF
        // ==========================================
        public void AuthorizeRolloutAndLaunch()
        {
            SpaceSimAudio.Instance.PlayVerifiedChime();

            if (currentMissionId == "SaturnV")
            {
                Debug.Log("<color=cyan><b>[Mission 1]</b></color> Saturn V rollout authorized! Transferring to launch complex...");
                // Start Rollout Sequence: Close Palette, switch camera to Launch Complex & enable Flight Controller
                if (blenderPalettePanel != null) blenderPalettePanel.SetActive(false);

                var flightCtrl = saturnVRoot.GetComponent<SaturnV.SaturnVFlightController>();
                if (flightCtrl != null)
                {
                    flightCtrl.enabled = true;
                }

                if (orbitCamera != null)
                {
                    orbitCamera.FocusOn(new Vector3(0, 15f, 0), 120f, 2.0f);
                }
            }
            else // Mission 2: Space Shuttle
            {
                Debug.Log("<color=orange><b>[Mission 2]</b></color> STS-107 authorized! Handing off to KC_MissionScene...");

                // Per Shivang's contract in spaceshattle mission2thisisbyshivang.md:
                // Check if KC_MissionScene is in BuildSettings, otherwise notify cleanly
                if (Application.CanStreamedLevelBeLoaded("KC_MissionScene"))
                {
                    SceneManager.LoadScene("KC_MissionScene");
                }
                else
                {
                    Debug.LogWarning("<color=yellow>[KC_MissionScene]</color> Waiting for collaborator's KC_MissionScene to finish merge. Staying in high-fidelity 3D inspection mode.");
                    if (_detailsBodyText != null)
                    {
                        _detailsBodyText.text = "<b><color=#00E5FF>STS-107 ROLLOUT AUTHORIZED</color></b>\n\n" +
                            "Space Shuttle Columbia is cleared for vertical rollout and flight deck handoff.\n" +
                            "Mission Scene hand-off contract: <i>Assets/_KalpanaMission/Scenes/KC_MissionScene</i>.\n\n" +
                            "All Shuttle propulsion, TPS thermal tiles, and flight deck systems inspected and nominal.";
                    }
                }
            }
        }

        // ==========================================
        // DYNAMIC UI BUILDER (UGUI)
        // ==========================================
        private void BuildDynamicUI()
        {
            if (mainCanvas == null)
            {
                var canvasGo = new GameObject("SpaceSim_MainCanvas");
                mainCanvas = canvasGo.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var cs = canvasGo.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Build Top Telemetry HUD
            BuildTopHUD(mainCanvas.transform, font);

            // 2. Build Mission Selection Panel
            BuildMissionSelectUI(mainCanvas.transform, font);

            // 3. Build Blender Palette Panel (Viewport Overlays + Right Dock)
            BuildBlenderPaletteUI(mainCanvas.transform, font);
        }

        private void BuildTopHUD(Transform parent, Font font)
        {
            var hudGo = new GameObject("TopHUD_Bar");
            hudGo.transform.SetParent(parent, false);

            var rt = hudGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 48);

            var img = hudGo.AddComponent<Image>();
            img.color = new Color(0.04f, 0.06f, 0.08f, 0.85f);

            // NASA Logo / Header
            var titleGo = new GameObject("HUD_Title");
            titleGo.transform.SetParent(hudGo.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 15;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(0.0f, 0.9f, 1.0f);
            titleText.text = " NASA AEROSPACE MISSION SIMULATOR  |  TELEMETRY CONSOLE";

            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0);
            titleRt.anchorMax = new Vector2(0.6f, 1);
            titleRt.offsetMin = new Vector2(20, 0);
            titleRt.offsetMax = Vector2.zero;

            // Help [?] Button
            var helpGo = CreateSimpleButton("HelpBtn", hudGo.transform, "[?] GUIDE", font, () =>
            {
                if (GameLoadingManager.Instance != null) GameLoadingManager.Instance.OpenTutorialModal();
            });
            var helpRt = helpGo.GetComponent<RectTransform>();
            helpRt.anchorMin = new Vector2(1, 0.5f);
            helpRt.anchorMax = new Vector2(1, 0.5f);
            helpRt.sizeDelta = new Vector2(90, 32);
            helpRt.anchoredPosition = new Vector2(-60, 0);
        }

        private void BuildMissionSelectUI(Transform parent, Font font)
        {
            missionSelectPanel = new GameObject("MissionSelectPanel");
            missionSelectPanel.transform.SetParent(parent, false);

            var rt = missionSelectPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = missionSelectPanel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.07f, 0.95f);

            // Header Banner
            var headerGo = new GameObject("HeaderLabel");
            headerGo.transform.SetParent(missionSelectPanel.transform, false);
            var headerText = headerGo.AddComponent<Text>();
            headerText.font = font;
            headerText.fontSize = 28;
            headerText.fontStyle = FontStyle.Bold;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.color = Color.white;
            headerText.text = "SELECT HISTORICAL SPACE MISSION";

            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 0.78f);
            headerRt.anchorMax = new Vector2(1, 0.88f);

            // Subheader
            var subGo = new GameObject("SubHeaderLabel");
            subGo.transform.SetParent(missionSelectPanel.transform, false);
            var subText = subGo.AddComponent<Text>();
            subText.font = font;
            subText.fontSize = 15;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.6f, 0.75f, 0.9f);
            subText.text = "Choose an expedition to inspect 3D flight hardware, review propulsion physics, and authorize launch.";

            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0, 0.72f);
            subRt.anchorMax = new Vector2(1, 0.78f);

            // Two Cards Layout
            var cardsGo = new GameObject("CardsContainer");
            cardsGo.transform.SetParent(missionSelectPanel.transform, false);
            var cardsRt = cardsGo.AddComponent<RectTransform>();
            cardsRt.anchorMin = new Vector2(0.1f, 0.15f);
            cardsRt.anchorMax = new Vector2(0.9f, 0.68f);
            cardsRt.offsetMin = Vector2.zero;
            cardsRt.offsetMax = Vector2.zero;

            var hlg = cardsGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Card 1: Apollo 11 Saturn V
            BuildMissionCard(cardsGo.transform, font,
                "MISSION 01",
                "APOLLO 11 — SATURN V",
                "Translunar Expedition & Lunar Landing",
                "• Date: July 16–24, 1969\n• Crew: Neil Armstrong, Michael Collins, Buzz Aldrin\n• Vehicle: Saturn V SA-506 (110.6 m / 2.96M kg)\n• Thrust: 35.1 MN (5x F-1 RP-1/LOX engines)\n• Target: Sea of Tranquility (Moon)",
                new Color(0.0f, 0.8f, 1.0f),
                () => SelectMission("SaturnV"));

            // Card 2: Space Shuttle Columbia
            BuildMissionCard(cardsGo.transform, font,
                "MISSION 02",
                "STS-107 — SPACE SHUTTLE",
                "Kalpana Chawla Memorial Science Mission",
                "• Date: Jan 16 – Feb 1, 2003\n• Crew: Rick Husband, Willie McCool, Dr. Kalpana Chawla\n• Vehicle: Columbia OV-102 (56.0 m / 2.05M kg)\n• Thrust: 30.16 MN (Twin SRBs + 3x RS-25 SSMEs)\n• Target: Low Earth Orbit (SPACEHAB Science)",
                new Color(1.0f, 0.55f, 0.1f),
                () => SelectMission("SpaceShuttle"));
        }

        private void BuildMissionCard(Transform parent, Font font, string tag, string title, string sub, string bullets, Color accent, UnityEngine.Events.UnityAction onSelect)
        {
            var cardGo = new GameObject("Card_" + tag);
            cardGo.transform.SetParent(parent, false);

            var img = cardGo.AddComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.13f, 0.95f);

            var outline = cardGo.AddComponent<Outline>();
            outline.effectColor = accent * 0.7f;
            outline.effectDistance = new Vector2(1, -1);

            var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Tag
            var tagGo = new GameObject("Tag");
            tagGo.transform.SetParent(cardGo.transform, false);
            var tagText = tagGo.AddComponent<Text>();
            tagText.font = font;
            tagText.fontSize = 12;
            tagText.fontStyle = FontStyle.Bold;
            tagText.color = accent;
            tagText.text = tag;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(cardGo.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.text = title;

            // Subtitle
            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(cardGo.transform, false);
            var subText = subGo.AddComponent<Text>();
            subText.font = font;
            subText.fontSize = 13;
            subText.color = new Color(0.7f, 0.8f, 0.9f);
            subText.text = sub;

            // Bullets
            var bulletsGo = new GameObject("Bullets");
            bulletsGo.transform.SetParent(cardGo.transform, false);
            var bulletsText = bulletsGo.AddComponent<Text>();
            bulletsText.font = font;
            bulletsText.fontSize = 14;
            bulletsText.lineSpacing = 1.3f;
            bulletsText.color = new Color(0.85f, 0.9f, 0.95f);
            bulletsText.text = bullets;

            var le = bulletsGo.AddComponent<LayoutElement>();
            le.minHeight = 130f;

            // Select Button
            var btnGo = CreateSimpleButton("SelectBtn", cardGo.transform, "INSPECT 3D VEHICLE & CONFIGURE ▸", font, onSelect);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.minHeight = 44f;
        }

        private void BuildBlenderPaletteUI(Transform parent, Font font)
        {
            blenderPalettePanel = new GameObject("BlenderPalettePanel");
            blenderPalettePanel.transform.SetParent(parent, false);

            var rt = blenderPalettePanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 1. Blender Viewport Overlay Gizmo / Controls Prompt
            var gizmoGo = new GameObject("ViewportControlsHint");
            gizmoGo.transform.SetParent(blenderPalettePanel.transform, false);
            var hintText = gizmoGo.AddComponent<Text>();
            hintText.font = font;
            hintText.fontSize = 12;
            hintText.color = new Color(0.6f, 0.7f, 0.8f, 0.8f);
            hintText.text = "<b>3D VIEWPORT:</b>  [Left Drag] Orbit  |  [Right/Mid Drag] Pan  |  [Scroll] Zoom";

            var hintRt = gizmoGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0, 0);
            hintRt.anchorMax = new Vector2(0.6f, 0);
            hintRt.offsetMin = new Vector2(25, 20);
            hintRt.offsetMax = new Vector2(0, 45);

            // Back to Missions Button
            _backToMissionsBtn = CreateSimpleButton("BackBtn", blenderPalettePanel.transform, "◂ MISSIONS DIRECTORY", font, ShowMissionSelect).GetComponent<Button>();
            var backRt = _backToMissionsBtn.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 1);
            backRt.anchorMax = new Vector2(0, 1);
            backRt.anchoredPosition = new Vector2(105, -75);
            backRt.sizeDelta = new Vector2(170, 36);

            // 2. Right Side Dock Panel (Width: 500px)
            var dockGo = new GameObject("RightDockPanel");
            dockGo.transform.SetParent(blenderPalettePanel.transform, false);

            var dockRt = dockGo.AddComponent<RectTransform>();
            dockRt.anchorMin = new Vector2(1, 0);
            dockRt.anchorMax = new Vector2(1, 1);
            dockRt.pivot = new Vector2(1, 0.5f);
            dockRt.sizeDelta = new Vector2(520, 0);
            dockRt.offsetMin = new Vector2(-520, 0);
            dockRt.offsetMax = new Vector2(0, -48); // Under top HUD

            var dockBg = dockGo.AddComponent<Image>();
            dockBg.color = new Color(0.05f, 0.07f, 0.10f, 0.94f);

            var dockOutline = dockGo.AddComponent<Outline>();
            dockOutline.effectColor = new Color(0.15f, 0.25f, 0.35f, 1f);
            dockOutline.effectDistance = new Vector2(-1, 0);

            // Dock Content Vertical Layout
            var vlg = dockGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Dock Header
            var headerGo = new GameObject("DockHeader");
            headerGo.transform.SetParent(dockGo.transform, false);
            _dockTitleText = headerGo.AddComponent<Text>();
            _dockTitleText.font = font;
            _dockTitleText.fontSize = 15;
            _dockTitleText.color = Color.white;
            _dockTitleText.text = "VEHICLE INSPECTION DOCK";

            // Progress text
            var progGo = new GameObject("DockProgress");
            progGo.transform.SetParent(dockGo.transform, false);
            _dockProgressText = progGo.AddComponent<Text>();
            _dockProgressText.font = font;
            _dockProgressText.fontSize = 12;
            _dockProgressText.color = new Color(0.0f, 0.9f, 1.0f);
            _dockProgressText.text = "SYSTEMS VERIFIED: 0 / 6";

            // Module Scroll View (Upper Dock)
            var scrollGo = new GameObject("ModuleScrollView");
            scrollGo.transform.SetParent(dockGo.transform, false);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.minHeight = 220f;
            scrollLe.preferredHeight = 230f;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewportGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0.03f, 0.04f, 0.06f, 0.5f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vpRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _moduleListContainer = contentGo.transform;
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 300);

            var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 6f;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = false;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;

            // Details Dossier Panel (Lower Dock)
            var detailsGo = new GameObject("DetailsDossierPanel");
            detailsGo.transform.SetParent(dockGo.transform, false);
            var detailsLe = detailsGo.AddComponent<LayoutElement>();
            detailsLe.minHeight = 360f;
            detailsLe.preferredHeight = 440f;
            detailsLe.flexibleHeight = 1f;

            var detailsImg = detailsGo.AddComponent<Image>();
            detailsImg.color = new Color(0.03f, 0.04f, 0.06f, 0.8f);

            var detailsScroll = detailsGo.AddComponent<ScrollRect>();
            detailsScroll.horizontal = false;
            detailsScroll.vertical = true;

            var dVpGo = new GameObject("DossierViewport");
            dVpGo.transform.SetParent(detailsGo.transform, false);
            var dVpRt = dVpGo.AddComponent<RectTransform>();
            dVpRt.anchorMin = Vector2.zero;
            dVpRt.anchorMax = Vector2.one;
            dVpRt.sizeDelta = Vector2.zero;
            dVpGo.AddComponent<Image>().color = Color.clear;
            dVpGo.AddComponent<Mask>().showMaskGraphic = false;
            detailsScroll.viewport = dVpRt;

            var dContentGo = new GameObject("DossierContent");
            dContentGo.transform.SetParent(dVpGo.transform, false);
            var dContentRt = dContentGo.AddComponent<RectTransform>();
            dContentRt.anchorMin = new Vector2(0, 1);
            dContentRt.anchorMax = new Vector2(1, 1);
            dContentRt.pivot = new Vector2(0.5f, 1);
            dContentRt.sizeDelta = new Vector2(0, 600);

            var dVlg = dContentGo.AddComponent<VerticalLayoutGroup>();
            dVlg.padding = new RectOffset(12, 12, 12, 12);
            dVlg.spacing = 8f;
            dVlg.childControlWidth = true;
            dVlg.childControlHeight = false;
            var dCsf = dContentGo.AddComponent<ContentSizeFitter>();
            dCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            detailsScroll.content = dContentRt;

            // Dossier Title
            var dtGo = new GameObject("DossierTitle");
            dtGo.transform.SetParent(dContentGo.transform, false);
            _detailsTitleText = dtGo.AddComponent<Text>();
            _detailsTitleText.font = font;
            _detailsTitleText.fontSize = 15;
            _detailsTitleText.fontStyle = FontStyle.Bold;
            _detailsTitleText.color = Color.white;
            _detailsTitleText.text = "SELECT A MODULE TO INSPECT";

            // Dossier Subtitle
            var dsubGo = new GameObject("DossierSubtitle");
            dsubGo.transform.SetParent(dContentGo.transform, false);
            _detailsSubtitleText = dsubGo.AddComponent<Text>();
            _detailsSubtitleText.font = font;
            _detailsSubtitleText.fontSize = 11;
            _detailsSubtitleText.color = new Color(0.7f, 0.8f, 0.9f);
            _detailsSubtitleText.text = "";

            // Dossier Body
            var dbodyGo = new GameObject("DossierBody");
            dbodyGo.transform.SetParent(dContentGo.transform, false);
            _detailsBodyText = dbodyGo.AddComponent<Text>();
            _detailsBodyText.font = font;
            _detailsBodyText.fontSize = 12;
            _detailsBodyText.lineSpacing = 1.25f;
            _detailsBodyText.color = new Color(0.85f, 0.88f, 0.92f);
            _detailsBodyText.text = "Click any rocket stage or subsystem above to examine its engineering specifications, thermodynamics, materials, and flight legacy.";

            // Bottom Launch / Rollout Button
            var launchBtnGo = CreateSimpleButton("RolloutLaunchBtn", dockGo.transform, "INSPECT ALL MODULES TO AUTHORIZE", font, AuthorizeRolloutAndLaunch);
            _rolloutButton = launchBtnGo.GetComponent<Button>();
            _rolloutButtonText = launchBtnGo.GetComponentInChildren<Text>();

            var launchLe = launchBtnGo.AddComponent<LayoutElement>();
            launchLe.minHeight = 48f;
            launchLe.preferredHeight = 48f;
        }

        private GameObject CreateSimpleButton(string name, Transform parent, string label, Font font, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.08f, 0.22f, 0.35f, 0.95f);

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.08f, 0.22f, 0.35f, 0.95f);
            colors.highlightedColor = new Color(0.0f, 0.5f, 0.75f, 1f);
            colors.pressedColor = new Color(0.0f, 0.7f, 0.95f, 1f);
            colors.disabledColor = new Color(0.1f, 0.12f, 0.15f, 0.7f);
            btn.colors = colors;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(onClick);
            return btnGo;
        }
    }
}
