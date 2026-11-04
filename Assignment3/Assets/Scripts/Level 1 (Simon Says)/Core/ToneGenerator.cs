using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// Builds simple AudioClips in code (tones, arpeggios, buzzes, noise bursts).
    /// No imported sound files are needed, so there is no copyright risk,
    /// and every sound can be tuned from code.
    /// </summary>
    public static class ToneGenerator
    {
        private const int SampleRate = 44100;

        /// <summary>A soft bell-like tone: a sine wave plus a quieter octave above it.</summary>
        public static AudioClip CreateTone(string name, float frequency, float duration, float volume = 0.5f)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t)
                           + 0.3f * Mathf.Sin(4f * Mathf.PI * frequency * t);
                data[i] = wave / 1.3f * Envelope(t, duration) * volume;
            }
            return Build(name, data);
        }

        /// <summary>Several tones played one after another (used for "round clear" and "solved").</summary>
        public static AudioClip CreateArpeggio(string name, float[] frequencies, float noteDuration, float volume = 0.5f)
        {
            int perNote = Mathf.CeilToInt(SampleRate * noteDuration);
            float[] data = new float[perNote * frequencies.Length];

            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / SampleRate;
                    float wave = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t);
                    data[n * perNote + i] = wave * Envelope(t, noteDuration) * volume;
                }
            }
            return Build(name, data);
        }

        /// <summary>A harsh "wrong" buzz: two slightly out-of-tune square waves.</summary>
        public static AudioClip CreateBuzz(string name, float frequency, float duration, float volume = 0.35f)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float a = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                float b = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * 1.06f * t));
                data[i] = (a + b) * 0.5f * Envelope(t, duration) * volume;
            }
            return Build(name, data);
        }

        /// <summary>A short burst of noise that dies away quickly (used as the gunshot).</summary>
        public static AudioClip CreateNoiseBurst(string name, float duration, float volume = 0.3f)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];
            var rng = new System.Random(12345);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float decay = Mathf.Exp(-t * 40f);
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * decay * volume;
            }
            return Build(name, data);
        }

        /// <summary>Quick fade-in and fade-out so clips start and end without clicks.</summary>
        private static float Envelope(float t, float duration)
        {
            const float attack = 0.01f;
            float release = Mathf.Min(0.12f, duration * 0.5f);
            float fadeIn = Mathf.Clamp01(t / attack);
            float fadeOut = Mathf.Clamp01((duration - t) / release);
            return fadeIn * fadeOut;
        }

        private static AudioClip Build(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
