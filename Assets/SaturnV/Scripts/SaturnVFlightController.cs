using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaturnV
{
    public class SaturnVFlightController : MonoBehaviour
    {
        [Header("Flight Parameters")]
        public float currentAltitude = 0f;
        public float velocity = 0f;
        public float acceleration = 9.8f;
        public bool isLaunched = false;
        public bool isIgnited = false;
        public float countdown = 5f;

        [Header("Engine Effects")]
        public List<ParticleSystem> engineFlames = new List<ParticleSystem>();
        public List<ParticleSystem> padSmokes = new List<ParticleSystem>();
        public List<Light> engineLights = new List<Light>();

        [Header("Cameras")]
        public Camera trackingCamera;
        public Camera onboardCamera;
        public int activeCameraIndex = 0;

        [Header("Audio")]
        public bool enableProceduralAudio = true;
        [Range(0f, 1f)] public float engineVolume = 0f;

        private float _initialY;
        private Rigidbody _rb;
        private AudioSource _audioSource;
        private System.Random _noiseGen = new System.Random();

        void Start()
        {
            _initialY = transform.position.y;
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.isKinematic = true;

            // Setup audio source for procedural rumble
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = true;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0.5f;
            _audioSource.minDistance = 20f;
            _audioSource.maxDistance = 1000f;
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }

            // Ensure engines start off
            SetEnginePower(0f);
        }

        void Update()
        {
            // Keyboard shortcuts (supports both New Input System and Legacy Input Manager)
            bool spacePressed = false;
            bool cPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
                cPressed = Keyboard.current.cKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            spacePressed = Input.GetKeyDown(KeyCode.Space);
            cPressed = Input.GetKeyDown(KeyCode.C);
#endif

            if (spacePressed && !isLaunched)
            {
                StartCoroutine(LaunchSequence());
            }

            if (cPressed)
            {
                SwitchCamera();
            }

            // Flight physics when launched
            if (isLaunched)
            {
                // Realistic Saturn V staging acceleration curve
                float gravity = 9.81f;
                // Thrust-to-weight ratio increases as fuel burns
                float thrustAcc = 14.5f + (currentAltitude / 1000f) * 2.5f;
                float netAcc = Mathf.Max(0.5f, thrustAcc - gravity);

                velocity += netAcc * Time.deltaTime;
                float deltaY = velocity * Time.deltaTime;
                transform.position += Vector3.up * deltaY;
                currentAltitude = transform.position.y - _initialY;

                // Subtle aerodynamic roll / pitch program
                if (currentAltitude > 80f)
                {
                    float pitchRate = Mathf.Clamp(currentAltitude * 0.005f, 0f, 25f);
                    transform.rotation = Quaternion.Euler(0f, 0f, -pitchRate);
                }

                // Engine flame flicker & light pulsing
                foreach (var light in engineLights)
                {
                    if (light != null)
                    {
                        light.intensity = 150f + Mathf.PerlinNoise(Time.time * 25f, 0f) * 80f;
                    }
                }

                engineVolume = Mathf.MoveTowards(engineVolume, 1.0f, Time.deltaTime * 0.5f);
            }
            else if (isIgnited)
            {
                // Engine buildup
                engineVolume = Mathf.MoveTowards(engineVolume, 0.9f, Time.deltaTime * 0.4f);
            }
        }

        public void TriggerLaunch()
        {
            if (!isLaunched)
            {
                StartCoroutine(LaunchSequence());
            }
        }

        public IEnumerator LaunchSequence()
        {
            isIgnited = true;
            // 1. Ignition phase
            SetEnginePower(0.5f);
            yield return new WaitForSeconds(2.0f);

            // 2. Full thrust & smoke release
            SetEnginePower(1.0f);
            yield return new WaitForSeconds(1.0f);

            // 3. Hold-down arms release -> Liftoff!
            isLaunched = true;
        }

        public void SetEnginePower(float power)
        {
            foreach (var ps in engineFlames)
            {
                if (ps != null)
                {
                    var emission = ps.emission;
                    emission.enabled = power > 0.05f;
                    emission.rateOverTime = 200f * power;
                    if (power > 0.05f && !ps.isPlaying) ps.Play();
                    else if (power <= 0.05f && ps.isPlaying) ps.Stop();
                }
            }

            foreach (var smoke in padSmokes)
            {
                if (smoke != null)
                {
                    var emission = smoke.emission;
                    emission.enabled = power > 0.1f;
                    emission.rateOverTime = 150f * power;
                    if (power > 0.1f && !smoke.isPlaying) smoke.Play();
                    else if (power <= 0.1f && smoke.isPlaying) smoke.Stop();
                }
            }

            foreach (var light in engineLights)
            {
                if (light != null)
                {
                    light.enabled = power > 0.05f;
                    light.intensity = 180f * power;
                }
            }
        }

        public void SwitchCamera()
        {
            activeCameraIndex = (activeCameraIndex + 1) % 2;
            if (trackingCamera != null) trackingCamera.enabled = (activeCameraIndex == 0);
            if (onboardCamera != null) onboardCamera.enabled = (activeCameraIndex == 1);
        }

        // Procedural low-frequency rocket rumble audio generator
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!enableProceduralAudio || engineVolume <= 0.01f) return;

            for (int i = 0; i < data.Length; i += channels)
            {
                // Generate turbulent pinkish noise + low harmonic rumble
                float white = (float)(_noiseGen.NextDouble() * 2.0 - 1.0);
                float rumble = Mathf.Sin((float)(Time.time * 120.0 + i)) * 0.3f
                             + Mathf.Sin((float)(Time.time * 65.0 + i)) * 0.4f;
                float sample = (white * 0.6f + rumble * 0.4f) * engineVolume * 0.4f;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] += sample;
                }
            }
        }

        void OnGUI()
        {
            // Mission Control HUD in Game View
            GUILayout.BeginArea(new Rect(20, 20, 260, 220), GUI.skin.box);
            GUILayout.Label("<size=15><b>APOLLO / SATURN V</b></size>");
            GUILayout.Space(5);

            if (!isLaunched)
            {
                GUI.color = Color.yellow;
                GUILayout.Label(isIgnited ? "STATUS: IGNITION SEQUENCE..." : "STATUS: ON PAD (READY)");
                GUI.color = Color.white;
                GUILayout.Space(5);
                if (GUILayout.Button(isIgnited ? "LAUNCH IN PROGRESS..." : "LAUNCH ROCKET (Space)", GUILayout.Height(35)))
                {
                    TriggerLaunch();
                }
            }
            else
            {
                GUI.color = Color.green;
                GUILayout.Label("STATUS: ASCENT PHASE");
                GUI.color = Color.white;
                GUILayout.Label(string.Format("Altitude: {0:F1} m", currentAltitude));
                GUILayout.Label(string.Format("Velocity: {0:F1} m/s ({1:F0} km/h)", velocity, velocity * 3.6f));
                GUILayout.Label(string.Format("Acceleration: {0:F1} g", (velocity > 0 ? (14.5f / 9.81f) : 1f)));
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Switch Camera (C)", GUILayout.Height(25)))
            {
                SwitchCamera();
            }
            GUILayout.Label("<size=10>Camera: " + (activeCameraIndex == 0 ? "Tracking Cam" : "Onboard Booster Cam") + "</size>");

            GUILayout.EndArea();
        }
    }
}
