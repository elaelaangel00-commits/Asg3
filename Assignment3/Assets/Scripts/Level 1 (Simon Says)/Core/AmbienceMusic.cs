using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// Non-diegetic background music for a level. Loops, fades in, and ducks while paused.
    /// If no clip is assigned, it generates a seamless low drone in code.
    /// persistAcrossScenes is OFF by default because every level in this project
    /// plays its own music; turn it on only if one track should carry over.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbienceMusic : MonoBehaviour
    {
        [Header("Track")]
        [SerializeField] private AudioClip musicClip;                    // empty = generated drone
        [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
        [SerializeField, Min(0f)] private float fadeTime = 2f;

        [Header("Behaviour")]
        [SerializeField] private bool persistAcrossScenes = false;
        [SerializeField, Range(0f, 1f)] private float pausedVolume = 0.3f;   // multiplier while paused

        private static AmbienceMusic instance;

        private AudioSource source;
        private AudioClip pendingClip;
        private bool switching;
        private float fade;          // 0..1 fade in/out
        private float pauseDuck = 1f;

        private void Awake()
        {
            // A persistent player already exists from an earlier scene: hand over the track and remove this one
            if (persistAcrossScenes && instance != null)
            {
                if (musicClip != null) instance.SwitchTo(musicClip);
                Destroy(gameObject);
                return;
            }

            if (persistAcrossScenes)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }

            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;               // 2D: non-diegetic
            source.volume = 0f;
            source.clip = musicClip != null ? musicClip : GenerateDrone();
            source.Play();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Fades out the current track, then fades in the new one.</summary>
        public void SwitchTo(AudioClip clip)
        {
            if (clip == null || clip == source.clip) return;
            pendingClip = clip;
            switching = true;
        }

        private void Update()
        {
            // Unscaled time: fades keep working while the game is paused (timeScale = 0)
            float dt = Time.unscaledDeltaTime;
            float step = fadeTime > 0f ? dt / fadeTime : 1f;

            if (switching)
            {
                fade = Mathf.MoveTowards(fade, 0f, step);
                if (fade <= 0f)
                {
                    source.clip = pendingClip;
                    source.Play();
                    switching = false;
                }
            }
            else
            {
                fade = Mathf.MoveTowards(fade, 1f, step);
            }

            float duckTarget = Time.timeScale == 0f ? pausedVolume : 1f;
            pauseDuck = Mathf.MoveTowards(pauseDuck, duckTarget, dt * 3f);

            source.volume = volume * fade * pauseDuck;
        }

        /// <summary>
        /// A 20-second low drone that loops with no click.
        /// Each frequency is rounded so it completes a whole number of cycles in 20 s,
        /// so the last sample joins smoothly onto the first.
        /// </summary>
        private static AudioClip GenerateDrone()
        {
            const int sampleRate = 44100;
            const float duration = 20f;

            float[] baseFrequencies = { 55f, 82.41f, 110f, 130.81f, 164.81f };   // A1 E2 A2 C3 E3 (A minor)
            float[] amplitudes = { 0.5f, 0.3f, 0.25f, 0.12f, 0.08f };

            float[] frequencies = new float[baseFrequencies.Length];
            for (int k = 0; k < baseFrequencies.Length; k++)
                frequencies[k] = Mathf.Round(baseFrequencies[k] * duration) / duration;

            int count = (int)(sampleRate * duration);
            float[] data = new float[count];
            double twoPi = 2.0 * System.Math.PI;

            for (int i = 0; i < count; i++)
            {
                double t = (double)i / sampleRate;
                double sample = 0.0;

                for (int k = 0; k < frequencies.Length; k++)
                {
                    // Each voice slowly swells; (k + 1) whole swells per loop keeps it seamless
                    double swell = 0.75 + 0.25 * System.Math.Sin(twoPi * (k + 1) / duration * t + k);
                    sample += amplitudes[k] * swell * System.Math.Sin(twoPi * frequencies[k] * t);
                }
                data[i] = (float)(sample * 0.5);
            }

            AudioClip clip = AudioClip.Create("AmbienceDrone", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
