using System.Collections.Generic;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// Pure logic, no scene objects: builds the random flash sequence.
    /// Rule: the same station never flashes twice in a row, because a double flash
    /// on one lamp is hard for the player to read.
    /// </summary>
    public static class SequenceGenerator
    {
        public static void Extend(List<int> sequence, int count, int optionCount)
        {
            if (optionCount <= 0)
            {
                Debug.LogError("SequenceGenerator: need at least one station.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int last = sequence.Count > 0 ? sequence[sequence.Count - 1] : -1;
                int next = Random.Range(0, optionCount);

                // If we rolled a repeat, shift by 1..(optionCount-1) and wrap around.
                // This always lands on a different station and needs no retry loop.
                if (next == last && optionCount > 1)
                    next = (next + Random.Range(1, optionCount)) % optionCount;

                sequence.Add(next);
            }
        }
    }
}
