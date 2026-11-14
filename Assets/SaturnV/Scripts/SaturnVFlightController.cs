using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaturnV
{
    /// <summary>
    /// Authentic Saturn V flight physics, gimbal attitude control (A/S/D/W),
    /// throttle management, multi-stage separation, and 4-view camera director.
    /// Follows the minimalist aerospace HUD specification from bymewhatiwant.md.
    /// </summary>
    public class SaturnVFlightController : MonoBehaviour
    {
        [Header("Flight Dynamics & Telemetry")]
        public float currentAltitude = 0f;
        public float velocity = 0f;
        public float throttle = 1.0f; // 0.0 to 1.0
        public float fuelPercentage = 100f;
        public float gForce = 1.0f;
        public float dynamicPressureQ = 0f;
        public int currentStage = 1; // 1: S-IC, 2: S-II, 3: S-IVB, 4: CSM Orbit

        [Header("Attitude & Gimbal (A/S/D/W)")]
        public Vector2 gimbalInput = Vector2.zero;
        public Vector3 currentAttitudeEuler = Vector3.zero;
        public float gimbalResponsiveness = 18f;

        [Header("Flight State")]
        public bool isLaunched = false;
        public bool isIgnited = false;
        public float countdown = 5f;

        [Header("Cameras (Key C)")]
        public Camera trackingCamera;
        public Camera chaseCamera;
        public Camera cockpitCamera;
        public Camera engineCamera;
        public Camera onboardCamera; // Compatibility alias
        public int activeCameraIndex = 0; // 0: Tracking, 1: Chase, 2: Cockpit, 3: Engine

        [Header("Stage Transforms")]
        public Transform stage1SIC;
        public Transform stage2SII;
        public Transform stage3SIVB;
        public Transform apolloCSM;
        public Transform launchEscapeSystem;

        [Header("Audio & FX")]
        public bool enableProceduralAudio = true;
        [Range(0f, 1f)] public float engineVolume = 0f;
        public List<Light> engineLights = new List<Light>();
        public List<ParticleSystem> engineFlames = new List<ParticleSystem>();
        public List<ParticleSystem> padSmokes = new List<ParticleSystem>();

        private float _initialY;
        private Rigidbody _rb;
        private AudioSource _audioSource;
        private System.Random _noiseGen = new System.Random();

        private void Start()
        {
            _initialY = transform.position.y;
            _rb = GetComponent<Rigidbody>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;

            // Setup audio source for procedural F-1 engine rumble
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = true;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0.5f;
            _audioSource.minDistance = 20f;
            _audioSource.maxDistance = 2000f;
            if (!_audioSource.isPlaying) _audioSource.Play();

            FindStageTransforms();
            SetupCameras();
        }

        private void FindStageTransforms()
        {
            stage1SIC = transform.Find("Saturn V First Stage") ?? transform.Find("Stage1_S-IC");
            stage2SII = transform.Find("Saturn V Second Stage") ?? transform.Find("Stage2_S-II");
            stage3SIVB = transform.Find("Saturn V Third Stage") ?? transform.Find("Stage3_S-IVB");
            apolloCSM = transform.Find("Apollo Capsule and SM") ?? transform.Find("Apollo_CSM");
            launchEscapeSystem = transform.Find("Launch Escape System") ?? transform.Find("Launch_Escape_System");
        }

        private void SetupCameras()
        {
            // 1. Ground Tracking Camera
            var trackGo = GameObject.Find("TrackingCamera");
            if (trackGo != null) trackingCamera = trackGo.GetComponent<Camera>();

            // 2. Orbital Chase Camera
            var chaseGo = GameObject.Find("ChaseCamera");
            if (chaseGo == null)
            {
                chaseGo = new GameObject("ChaseCamera");
                chaseCamera = chaseGo.AddComponent<Camera>();
                chaseCamera.enabled = false;
            }
            else chaseCamera = chaseGo.GetComponent<Camera>();

            // 3. Cockpit Interior Camera
            var cockpitGo = GameObject.Find("CockpitCamera");
            if (cockpitGo == null)
            {
                cockpitGo = new GameObject("CockpitCamera");
                cockpitGo.transform.SetParent(transform, false);
                cockpitGo.transform.localPosition = new Vector3(0f, 99f, 0.4f);
                cockpitCamera = cockpitGo.AddComponent<Camera>();
                cockpitCamera.fieldOfView = 65f;
                cockpitCamera.enabled = false;
            }
            else cockpitCamera = cockpitGo.GetComponent<Camera>();

            // 4. Engine Camera
            var engineCamGo = GameObject.Find("EngineExhaustCamera");
            if (engineCamGo == null)
            {
                engineCamGo = new GameObject("EngineExhaustCamera");
                engineCamGo.transform.SetParent(transform, false);
                engineCamGo.transform.localPosition = new Vector3(6f, -1f, 6f);
                engineCamGo.transform.localRotation = Quaternion.Euler(-55f, -135f, 0f);
                engineCamera = engineCamGo.AddComponent<Camera>();
                engineCamera.enabled = false;
            }
            else engineCamera = engineCamGo.GetComponent<Camera>();
        }

        private void Update()
        {
            HandleInputs();

            if (isLaunched)
            {
                UpdateFlightPhysics();
            }
            else if (isIgnited)
            {
                engineVolume = Mathf.MoveTowards(engineVolume, 0.85f, Time.deltaTime * 0.5f);
            }

            UpdateChaseCamera();
        }

        private void HandleInputs()
        {
            float horizontal = 0f;
            float vertical = 0f;
            bool spacePressed = false;
            bool cPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;

                if (Keyboard.current.shiftKey.isPressed) throttle = Mathf.Clamp01(throttle + Time.deltaTime * 0.4f);
                if (Keyboard.current.ctrlKey.isPressed) throttle = Mathf.Clamp01(throttle - Time.deltaTime * 0.4f);

                spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
                cPressed = Keyboard.current.cKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
            if (Input.GetKey(KeyCode.LeftShift)) throttle = Mathf.Clamp01(throttle + Time.deltaTime * 0.4f);
            if (Input.GetKey(KeyCode.LeftControl)) throttle = Mathf.Clamp01(throttle - Time.deltaTime * 0.4f);
            spacePressed = Input.GetKeyDown(KeyCode.Space);
            cPressed = Input.GetKeyDown(KeyCode.C);
#endif

            gimbalInput = Vector2.Lerp(gimbalInput, new Vector2(horizontal, vertical), Time.deltaTime * gimbalResponsiveness);

            if (cPressed)
            {
                SwitchCamera();
            }

            if (spacePressed)
            {
                if (!isLaunched)
                {
                    StartCoroutine(LaunchSequence());
                }
                else if (fuelPercentage <= 15f || currentAltitude > 65000f)
                {
                    TriggerNextStage();
                }
            }
        }

        private void UpdateFlightPhysics()
        {
            // Fuel Burn
            float burnRate = (currentStage == 1 ? 0.60f : (currentStage == 2 ? 0.28f : 0.17f)) * throttle;
            fuelPercentage = Mathf.Max(0f, fuelPercentage - burnRate * Time.deltaTime);

            // TWR & Acceleration
            float stageBaseThrust = (currentStage == 1 ? 14.8f : (currentStage == 2 ? 16.5f : 18.2f));
            float gravity = 9.81f * Mathf.Clamp01(1f - (currentAltitude / 400000f));
            float actualThrust = (fuelPercentage > 0f ? (stageBaseThrust * throttle) : 0f);
            float netAcc = actualThrust - gravity;

            gForce = Mathf.Max(1.0f, actualThrust / 9.81f);
            velocity = Mathf.Max(0f, velocity + netAcc * Time.deltaTime);

            // Position & Altitude
            Vector3 flightDirection = transform.up;
            transform.position += flightDirection * (velocity * Time.deltaTime);
            currentAltitude = Mathf.Max(0f, transform.position.y - _initialY);

            // Dynamic Pressure (Max-Q around 11-14 km)
            float airDensity = Mathf.Exp(-currentAltitude / 8500f);
            dynamicPressureQ = 0.5f * airDensity * (velocity * velocity) / 1000f; // kPa

            // Attitude Steering (Programmed gravity turn + user A/S/D/W manual gimbal trim)
            float targetPitch = Mathf.Clamp((currentAltitude / 150000f) * 78f, 0f, 78f);
            targetPitch += gimbalInput.y * 12f;
            float targetYaw = gimbalInput.x * 12f;

            currentAttitudeEuler = Vector3.Lerp(currentAttitudeEuler, new Vector3(targetPitch, targetYaw, 0f), Time.deltaTime * 2.5f);
            transform.rotation = Quaternion.Euler(currentAttitudeEuler.x, currentAttitudeEuler.y, 0f);

            // Audio & visual engine modulation
            engineVolume = Mathf.MoveTowards(engineVolume, (fuelPercentage > 0 ? throttle : 0f), Time.deltaTime * 0.8f);

            // Auto-trigger Stage 1 separation at 68 km if not already separated
            if (currentStage == 1 && (fuelPercentage <= 0.1f || currentAltitude >= 68000f))
            {
                TriggerNextStage();
            }
        }

        public void TriggerNextStage()
        {
            if (currentStage == 1)
            {
                // Stage 1 (S-IC) Separation
                currentStage = 2;
                fuelPercentage = 100f;
                if (stage1SIC != null)
                {
                    stage1SIC.SetParent(null); // Jettison stage 1
                    var s1Rb = stage1SIC.gameObject.AddComponent<Rigidbody>();
                    s1Rb.linearVelocity = transform.up * (velocity * 0.85f) - Vector3.up * 8f;
                    Destroy(stage1SIC.gameObject, 15f);
                }
                Debug.Log("<color=cyan>[Saturn V]</color> Stage 1 (S-IC) Separated! S-II Cryogenic Stage Ignited.");
            }
            else if (currentStage == 2)
            {
                // Stage 2 (S-II) Separation & LES Tower Jettison
                currentStage = 3;
                fuelPercentage = 100f;
                if (launchEscapeSystem != null)
                {
                    launchEscapeSystem.SetParent(null);
                    var lesRb = launchEscapeSystem.gameObject.AddComponent<Rigidbody>();
                    lesRb.linearVelocity = transform.up * (velocity * 1.3f) + Vector3.right * 15f;
                    Destroy(launchEscapeSystem.gameObject, 10f);
                }

                if (stage2SII != null)
                {
                    stage2SII.SetParent(null);
                    var s2Rb = stage2SII.gameObject.AddComponent<Rigidbody>();
                    s2Rb.linearVelocity = transform.up * (velocity * 0.9f) - Vector3.up * 6f;
                    Destroy(stage2SII.gameObject, 15f);
                }
                Debug.Log("<color=cyan>[Saturn V]</color> Stage 2 (S-II) & LES Jettisoned! S-IVB Orbital Insertion Burn Started.");
            }
            else if (currentStage == 3)
            {
                // Orbit Insertion Complete -> Translunar Coast
                currentStage = 4;
                Debug.Log("<color=green>[Saturn V]</color> Parking Orbit 185 km Achieved! S-IVB Ready for Translunar Injection (TLI).");
            }
        }

        public IEnumerator LaunchSequence()
        {
            isIgnited = true;
            yield return new WaitForSeconds(2.0f); // F-1 Engine Ignition Build-up
            isLaunched = true;
        }

        public void SwitchCamera()
        {
            activeCameraIndex = (activeCameraIndex + 1) % 4;

            if (trackingCamera != null) trackingCamera.enabled = (activeCameraIndex == 0);
            if (chaseCamera != null) chaseCamera.enabled = (activeCameraIndex == 1);
            if (cockpitCamera != null) cockpitCamera.enabled = (activeCameraIndex == 2);
            if (engineCamera != null) engineCamera.enabled = (activeCameraIndex == 3);
        }

        private void UpdateChaseCamera()
        {
            if (chaseCamera == null || !chaseCamera.enabled) return;

            Vector3 targetPos = transform.position - transform.up * 35f + Vector3.back * 65f + Vector3.up * 18f;
            chaseCamera.transform.position = Vector3.Lerp(chaseCamera.transform.position, targetPos, Time.deltaTime * 5f);
            chaseCamera.transform.LookAt(transform.position + transform.up * 20f);
        }

        // Procedural audio synthesizer for Rocketdyne F-1 engine rumble
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!enableProceduralAudio || engineVolume <= 0.01f) return;

            for (int i = 0; i < data.Length; i += channels)
            {
                float white = (float)(_noiseGen.NextDouble() * 2.0 - 1.0);
                float rumble = Mathf.Sin((float)(Time.time * 110.0 + i)) * 0.35f
                             + Mathf.Sin((float)(Time.time * 58.0 + i)) * 0.45f;
                float sample = (white * 0.55f + rumble * 0.45f) * engineVolume * 0.38f;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] += sample;
                }
            }
        }

        private void OnGUI()
        {
            if (!isLaunched && !isIgnited) return;

            // Minimalist Dark Aerospace Theme: Deep Charcoal (#0B0E14), Slate (#1E293B), Muted Cyan (#38BDF8)
            var originalColor = GUI.color;

            // 1. LEFT SIDE: Attitude Steering & Joystick Indicator (A/S/D/W)
            Rect leftArea = new Rect(24, Screen.height - 240, 240, 210);
            GUI.Box(leftArea, "");
            GUILayout.BeginArea(new Rect(leftArea.x + 12, leftArea.y + 10, leftArea.width - 24, leftArea.height - 20));

            GUILayout.Label("<b><color=#38BDF8>ATTITUDE GIMBAL</color></b> <size=10><color=#94A3B8>[A/S/D/W]</color></size>");
            GUILayout.Space(4);
            GUILayout.Label($"Pitch Program: <color=#F8FAFC>{currentAttitudeEuler.x:F1}°</color>");
            GUILayout.Label($"Yaw Steering:  <color=#F8FAFC>{currentAttitudeEuler.y:F1}°</color>");

            // Visual Crosshair / Gimbal Box
            Rect crosshairRect = GUILayoutUtility.GetRect(70, 70);
            GUI.Box(crosshairRect, "");
            Vector2 center = crosshairRect.center;
            Vector2 stickPos = center + new Vector2(gimbalInput.x * 25f, -gimbalInput.y * 25f);
            GUI.color = new Color(0.22f, 0.74f, 0.97f, 0.85f);
            GUI.Box(new Rect(stickPos.x - 5, stickPos.y - 5, 10, 10), "");
            GUI.color = originalColor;

            GUILayout.Label("<size=10><color=#94A3B8>A/D: Yaw  |  W/S: Pitch</color></size>");
            GUILayout.EndArea();

            // 2. RIGHT SIDE: Speed / Throttle Control & Staging
            Rect rightArea = new Rect(Screen.width - 264, Screen.height - 260, 240, 230);
            GUI.Box(rightArea, "");
            GUILayout.BeginArea(new Rect(rightArea.x + 12, rightArea.y + 10, rightArea.width - 24, rightArea.height - 20));

            GUILayout.Label("<b><color=#38BDF8>PROPULSION & SPEED</color></b>");
            GUILayout.Space(2);
            GUILayout.Label($"Throttle: <color=#F8FAFC>{throttle * 100f:F0}%</color>");
            throttle = GUILayout.HorizontalSlider(throttle, 0f, 1f);

            GUILayout.Space(4);
            GUILayout.Label($"Fuel Remaining: <color=#F8FAFC>{fuelPercentage:F1}%</color>");
            GUILayout.Label($"Active Stage: <color=#38BDF8>STAGE {currentStage}</color>");

            GUILayout.Space(4);
            if (GUILayout.Button(currentStage < 4 ? $"STAGE SEPARATION (Space)" : "ORBIT NOMINAL", GUILayout.Height(30)))
            {
                TriggerNextStage();
            }

            GUILayout.Label("<size=10><color=#94A3B8>Shift: Speed Up | Ctrl: Slow Down</color></size>");
            GUILayout.EndArea();

            // 3. TOP TELEMETRY STRIP
            Rect topArea = new Rect(Screen.width * 0.5f - 240, 16, 480, 58);
            GUI.Box(topArea, "");
            GUILayout.BeginArea(new Rect(topArea.x + 12, topArea.y + 8, topArea.width - 24, topArea.height - 16));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>ALT:</b> <color=#38BDF8>{(currentAltitude > 1000 ? $"{currentAltitude / 1000f:F2} km" : $"{currentAltitude:F0} m")}</color>");
            GUILayout.Label($"<b>VEL:</b> <color=#38BDF8>{velocity:F0} m/s</color> <size=10>({velocity * 3.6f:F0} km/h)</size>");
            GUILayout.Label($"<b>G-FORCE:</b> <color=#38BDF8>{gForce:F1} g</color>");
            GUILayout.Label($"<b>MAX-Q:</b> <color=#38BDF8>{dynamicPressureQ:F1} kPa</color>");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string camName = (activeCameraIndex == 0 ? "Tracking Cam" : (activeCameraIndex == 1 ? "Chase Cam" : (activeCameraIndex == 2 ? "Cockpit Cam" : "Engine Cam")));
            if (GUILayout.Button($"Camera: {camName} [C]", GUILayout.Height(20)))
            {
                SwitchCamera();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            GUI.color = originalColor;
        }
    }
}
