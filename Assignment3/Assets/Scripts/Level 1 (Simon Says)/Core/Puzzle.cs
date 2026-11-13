using System;
using UnityEngine;

namespace Backrooms.Core
{
    // base class for every puzzle.
    // a puzzle only decides WHEN it is solved (CheckConditions).
    // doors and ui just listen to the Solved / Unsolved events,
    // so they never need to know how the puzzle works.
    public abstract class Puzzle : MonoBehaviour
    {
        [SerializeField] private string puzzleName = "Puzzle";

        // events other scripts can listen to
        public event Action<Puzzle> Solved;
        public event Action<Puzzle> Unsolved;

        public string PuzzleName => puzzleName;
        public bool IsSolved { get; private set; }   // anyone can read it, only this class can change it

        // each puzzle writes its own rule for "am i solved?"
        protected abstract bool CheckConditions();

        // call this whenever the puzzle changes.
        // the events only fire when solved actually flips (false -> true or true -> false).
        protected void Evaluate()
        {
            bool nowSolved = CheckConditions();
            if (nowSolved == IsSolved) return;   // nothing changed, do nothing

            IsSolved = nowSolved;
            if (nowSolved) Solved?.Invoke(this);   // ?. = only call if someone is listening
            else Unsolved?.Invoke(this);
        }
    }
}
