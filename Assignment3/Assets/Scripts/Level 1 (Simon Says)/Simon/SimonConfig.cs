using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// Every tuning number for the Simon Says puzzle lives here, in an asset.
    /// Difficulty can be changed in the Inspector without touching any code.
    /// </summary>
    [CreateAssetMenu(fileName = "SimonConfig", menuName = "Backrooms/Simon Config")]
    public class SimonConfig : ScriptableObject
    {
        [Header("Rounds")]
        [Min(1)] public int rounds = 3;
        [Min(1)] public int startLength = 3;            // flashes in round 1
        [Min(1)] public int stepsAddedPerRound = 1;     // round 2 = 4 flashes, round 3 = 5
        public bool restartAutomatically = true;        // false = player must shoot START again after failing

        [Header("Timing (seconds)")]
        [Min(0f)] public float leadIn = 0.8f;           // pause before the first flash
        [Min(0f)] public float betweenRounds = 1.5f;
        [Min(0f)] public float failDelay = 2f;
        [Min(0.5f)] public float inputTimeout = 4f;     // time allowed between correct shots
        public float[] flashPerRound = { 0.6f, 0.45f, 0.35f };
        public float[] gapPerRound = { 0.3f, 0.25f, 0.2f };

        [Header("Stations (index 0 = leftmost)")]
        public Color[] stationColours =
        {
            new Color(1f, 0.55f, 0.1f),   // orange
            new Color(1f, 0.9f, 0.2f),    // yellow
            new Color(0.2f, 0.9f, 0.3f),  // green
            new Color(0.2f, 0.6f, 1f),    // blue
            new Color(0.75f, 0.3f, 1f)    // purple
        };

        // C major pentatonic (C5 D5 E5 G5 A5): any random order still sounds like a tune
        public float[] stationFrequencies = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };

        [Header("Feedback colours")]
        public Color watchColour = new Color(1f, 0.9f, 0.4f);
        public Color inputColour = new Color(0.4f, 0.9f, 1f);
        public Color successColour = new Color(0.3f, 1f, 0.4f);
        public Color failColour = new Color(1f, 0.15f, 0.15f);

        public float FlashFor(int round) => PickByRound(flashPerRound, round, 0.5f);
        public float GapFor(int round) => PickByRound(gapPerRound, round, 0.25f);

        public Color ColourFor(int index)
        {
            if (stationColours != null && index < stationColours.Length) return stationColours[index];
            return Color.HSVToRGB((index * 0.2f) % 1f, 0.8f, 1f);       // fallback if the array is short
        }

        public float FrequencyFor(int index)
        {
            if (stationFrequencies != null && index < stationFrequencies.Length) return stationFrequencies[index];
            return 440f * Mathf.Pow(2f, index / 12f);                    // fallback: semitones above A4
        }

        /// <summary>Gets the value for this round; later rounds reuse the last value.</summary>
        private static float PickByRound(float[] values, int round, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Mathf.Clamp(round, 0, values.Length - 1)];
        }
    }
}
