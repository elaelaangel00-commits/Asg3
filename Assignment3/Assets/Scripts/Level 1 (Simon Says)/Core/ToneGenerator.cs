using UnityEngine;

namespace Backrooms.Core
{
    // makes sounds in code instead of using audio files.
    // a sound is just a list of numbers (samples) between -1 and 1.
    // we fill that list with waves, then turn it into an AudioClip.
    public static class ToneGenerator
    {
        private const int SampleRate = 44100;   // 44100 samples per second (cd quality)

        // soft bell tone: a sine wave plus a quieter wave one octave higher
        public static AudioClip CreateTone(string name, float frequency, float duration, float volume = 0.5f)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);   // how many samples we need
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;   // time in seconds for this sample
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t)
                           + 0.3f * Mathf.Sin(4f * Mathf.PI * frequency * t);
                data[i] = wave / 1.3f * Envelope(t, duration) * volume;
            }
            return Build(name, data);
        }

        // a few notes played one after another (round clear / solved jingle)
        public static AudioClip CreateArpeggio(string name, float[] frequencies, float noteDuration, float volume = 0.5f)
        {
            int perNote = Mathf.CeilToInt(SampleRate * noteDuration);
            float[] data = new float[perNote * frequencies.Length];

            for (int n = 0; n < frequencies.Length; n++)       // each note
            {
                for (int i = 0; i < perNote; i++)              // each sample in that note
                {
                    float t = (float)i / SampleRate;
                    float wave = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t);
                    data[n * perNote + i] = wave * Envelope(t, noteDuration) * volume;
                }
            }
            return Build(name, data);
        }

        // harsh "wrong" buzz: two square waves slightly out of tune
        public static AudioClip CreateBuzz(string name, float frequency, float duration, float volume = 0.35f)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float a = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));           // sign() turns a sine into a square wave
                float b = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * 1.06f * t));   // 6% higher = clashing sound
                data[i] = (a + b) * 0.5f * Envelope(t, duration) * volume;
            }
            return Build(name, data);
        }

        // short burst of random noise that fades fast (the gunshot)
        public static AudioClip CreateNoiseBurst(string name, float duration, float volume = 0.3f)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];
            var rng = new System.Random(12345);   // fixed seed = same shot sound every time

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float decay = Mathf.Exp(-t * 40f);   // dies away quickly
                data[i] = ((float)rng.NextDouble() * 2f - 1f) * decay * volume;
            }
            return Build(name, data);
        }

        // quick fade in and fade out so the sound doesn't click at the start/end
        private static float Envelope(float t, float duration)
        {
            const float attack = 0.01f;
            float release = Mathf.Min(0.12f, duration * 0.5f);
            float fadeIn = Mathf.Clamp01(t / attack);
            float fadeOut = Mathf.Clamp01((duration - t) / release);
            return fadeIn * fadeOut;
        }

        // turns the list of samples into a real AudioClip unity can play
        private static AudioClip Build(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);   // 1 = mono
            clip.SetData(data, 0);
            return clip;
        }
    }
}
