using UnityEngine;

namespace SpaceSim.Core
{
    /// <summary>
    /// Procedural audio generator for authentic NASA flight telemetry and UI feedback.
    /// Synthesizes UI clicks, telemetry beeps, Morse tones, and verification chimes at runtime
    /// without relying on external audio file dependencies.
    /// </summary>
    public class SpaceSimAudio : MonoBehaviour
    {
        private static SpaceSimAudio _instance;
        public static SpaceSimAudio Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SpaceSimAudio");
                    _instance = go.AddComponent<SpaceSimAudio>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private AudioSource _source;
        public bool Muted { get; set; } = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public void PlayClick()
        {
            if (Muted) return;
            PlayTone(1200f, 0.035f, 0.25f, WaveType.Sine);
        }

        public void PlayBeep(float freq = 980f, float duration = 0.06f, float volume = 0.35f)
        {
            if (Muted) return;
            PlayTone(freq, duration, volume, WaveType.Sine);
        }

        public void PlaySelect()
        {
            if (Muted) return;
            PlayTone(1450f, 0.05f, 0.3f, WaveType.Sine);
        }

        public void PlayVerifiedChime()
        {
            if (Muted) return;
            StartCoroutine(PlayChimeSequence());
        }

        private System.Collections.IEnumerator PlayChimeSequence()
        {
            PlayTone(880f, 0.08f, 0.3f, WaveType.Sine);
            yield return new WaitForSeconds(0.09f);
            PlayTone(1174.66f, 0.12f, 0.35f, WaveType.Sine);
            yield return new WaitForSeconds(0.13f);
            PlayTone(1760f, 0.22f, 0.4f, WaveType.Sine);
        }

        public void PlayError()
        {
            if (Muted) return;
            StartCoroutine(PlayErrorSequence());
        }

        private System.Collections.IEnumerator PlayErrorSequence()
        {
            PlayTone(440f, 0.12f, 0.4f, WaveType.Square);
            yield return new WaitForSeconds(0.14f);
            PlayTone(330f, 0.2f, 0.45f, WaveType.Square);
        }

        private enum WaveType { Sine, Square }

        private void PlayTone(float freq, float duration, float volume, WaveType type)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            float phase = 0f;
            float phaseIncrement = (2f * Mathf.PI * freq) / sampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                // Attack-Decay envelope to avoid audio clicks
                float envelope = Mathf.Sin(t * Mathf.PI);

                float wave = 0f;
                if (type == WaveType.Sine)
                {
                    wave = Mathf.Sin(phase);
                }
                else
                {
                    wave = Mathf.Sin(phase) >= 0 ? 0.7f : -0.7f;
                }

                samples[i] = wave * envelope * volume;
                phase += phaseIncrement;
            }

            AudioClip clip = AudioClip.Create("Tone_" + freq, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            _source.PlayOneShot(clip);
        }
    }
}
