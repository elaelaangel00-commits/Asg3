using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    // all the settings for the simon says puzzle, saved as an asset.
    // a ScriptableObject is a data file: we change difficulty in the inspector
    // without touching any code (no hard-coded numbers).
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
        public float[] flashPerRound = { 0.6f, 0.45f, 0.35f };   // lamp on-time, gets faster each round
        public float[] gapPerRound = { 0.3f, 0.25f, 0.2f };      // gap between flashes

        [Header("Stations (index 0 = leftmost)")]
        public Color[] stationColours =
        {
            new Color(1f, 0.55f, 0.1f),   // orange
            new Color(1f, 0.9f, 0.2f),    // yellow
            new Color(0.2f, 0.9f, 0.3f),  // green
            new Color(0.2f, 0.6f, 1f),    // blue
            new Color(0.75f, 0.3f, 1f)    // purple
        };

        // notes c d e g a (pentatonic scale): any random order still sounds like a tune
        public float[] stationFrequencies = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };

        [Header("Feedback colours")]
        public Color watchColour = new Color(1f, 0.9f, 0.4f);
        public Color inputColour = new Color(0.4f, 0.9f, 1f);
        public Color successColour = new Color(0.3f, 1f, 0.4f);
        public Color failColour = new Color(1f, 0.15f, 0.15f);

        // => is a short way to write a method that returns one value
        public float FlashFor(int round) => PickByRound(flashPerRound, round, 0.5f);
        public float GapFor(int round) => PickByRound(gapPerRound, round, 0.25f);

        public Color ColourFor(int index)
        {
            if (stationColours != null && index < stationColours.Length) return stationColours[index];
            return Color.HSVToRGB((index * 0.2f) % 1f, 0.8f, 1f);       // backup if the array is too short
        }

        public float FrequencyFor(int index)
        {
            if (stationFrequencies != null && index < stationFrequencies.Length) return stationFrequencies[index];
            return 440f * Mathf.Pow(2f, index / 12f);                    // backup: go up one note at a time
        }

        // gets the value for this round. if there are more rounds than values, reuse the last one.
        private static float PickByRound(float[] values, int round, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Mathf.Clamp(round, 0, values.Length - 1)];
        }
    }
}
