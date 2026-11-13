using System.Collections.Generic;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    // builds the random flash order.
    // pure logic, doesn't touch any scene objects.
    // rule: the same lamp never flashes twice in a row (hard for the player to see).
    public static class SequenceGenerator
    {
        // adds "count" new random steps to the end of the sequence
        public static void Extend(List<int> sequence, int count, int optionCount)
        {
            if (optionCount <= 0)
            {
                Debug.LogError("SequenceGenerator: need at least one station.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int last = sequence.Count > 0 ? sequence[sequence.Count - 1] : -1;   // -1 = no last step yet
                int next = Random.Range(0, optionCount);                            // 0 to optionCount-1

                // rolled the same lamp as last time? shift it along by 1 to (optionCount-1)
                // and wrap around with %. this always gives a different lamp, no retry loop needed.
                if (next == last && optionCount > 1)
                    next = (next + Random.Range(1, optionCount)) % optionCount;

                sequence.Add(next);
            }
        }
    }
}
