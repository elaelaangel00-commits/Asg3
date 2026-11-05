using System;
using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// Base class for every puzzle in the game.
    /// A subclass only decides WHEN it is solved (CheckConditions).
    /// Doors, the HUD and the PuzzleManager only listen to the Solved / Unsolved events,
    /// so they never need to know how any individual puzzle works.
    /// </summary>
    public abstract class Puzzle : MonoBehaviour
    {
        [SerializeField] private string puzzleName = "Puzzle";

        public event Action<Puzzle> Solved;
        public event Action<Puzzle> Unsolved;

        public string PuzzleName => puzzleName;
        public bool IsSolved { get; private set; }

        /// <summary>Return true when every condition of this puzzle is met.</summary>
        protected abstract bool CheckConditions();

        /// <summary>
        /// Call this whenever the puzzle's state changes.
        /// Events only fire when the solved state actually flips.
        /// </summary>
        protected void Evaluate()
        {
            bool nowSolved = CheckConditions();
            if (nowSolved == IsSolved) return;

            IsSolved = nowSolved;
            if (nowSolved) Solved?.Invoke(this);
            else Unsolved?.Invoke(this);
        }
    }
}
