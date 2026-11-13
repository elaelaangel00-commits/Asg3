using UnityEngine;

namespace Backrooms.Core
{
    // background music for the level (non-diegetic: only the player hears it).
    // loops, fades in at the start, and gets quieter while the game is paused.
    // if no clip is given, it makes a low drone in code.
    // persistAcrossScenes is off because every level plays its own music.
    [RequireComponent(typeof(AudioSource))]
    public class AmbienceMusic : MonoBehaviour
    {
        [Header("Track")]
        [SerializeField] private AudioClip musicClip;                    // empty = generated drone
        [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
        [SerializeField, Min(0f)] private float fadeTime = 2f;           // seconds to fade in / out

        [Header("Behaviour")]
        [SerializeField] private bool persistAcrossScenes = false;
        [SerializeField, Range(0f, 1f)] private float pausedVolume = 0.3f;   // volume multiplier while paused

        private static AmbienceMusic instance;   // the one music player that survives scene changes (if persist is on)

        private AudioSource source;
        private AudioClip pendingClip;   // next track to play when switching
        private bool switching;
        private float fade;              // 0 = silent, 1 = full volume
        private float pauseDuck = 1f;    // 1 = normal, lower = paused

        private void Awake()
        {
            // a music player already exists from an earlier scene:
            // give it our track (if different) and delete this copy
            if (persistAcrossScenes && instance != null)
            {
                if (musicClip != null) instance.SwitchTo(musicClip);
                Destroy(gameObject);
                return;
            }

            if (persistAcrossScenes)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);   // keep this object when a new scene loads
            }

            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;               // 0 = 2d sound, same volume everywhere
            source.volume = 0f;                     // start silent, then fade in
            source.clip = musicClip != null ? musicClip : GenerateDrone();
            source.Play();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // fade out the current track, then fade in the new one
        public void SwitchTo(AudioClip clip)
        {
            if (clip == null || clip == source.clip) return;
            pendingClip = clip;
            switching = true;
        }

        private void Update()
        {
            // unscaled time keeps working even when the game is paused (timeScale = 0)
            float dt = Time.unscaledDeltaTime;
            float step = fadeTime > 0f ? dt / fadeTime : 1f;

            if (switching)
            {
                // fade down to 0, then swap the clip
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
                fade = Mathf.MoveTowards(fade, 1f, step);   // fade up to full
            }

            // smoothly lower the music while paused, raise it again after
            float duckTarget = Time.timeScale == 0f ? pausedVolume : 1f;
            pauseDuck = Mathf.MoveTowards(pauseDuck, duckTarget, dt * 3f);

            source.volume = volume * fade * pauseDuck;
        }

        // makes a 20 second low drone that loops with no click.
        // each note is rounded so it finishes a whole number of waves in 20s,
        // so the end of the clip joins smoothly back to the start.
        private static AudioClip GenerateDrone()
        {
            const int sampleRate = 44100;
            const float duration = 20f;

            float[] baseFrequencies = { 55f, 82.41f, 110f, 130.81f, 164.81f };   // A1 E2 A2 C3 E3 (a minor chord)
            float[] amplitudes = { 0.5f, 0.3f, 0.25f, 0.12f, 0.08f };            // lower notes louder

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

                // add all the notes together
                for (int k = 0; k < frequencies.Length; k++)
                {
                    // each note slowly gets louder and softer ("breathing").
                    // (k + 1) full swells per loop keeps the loop seamless.
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
