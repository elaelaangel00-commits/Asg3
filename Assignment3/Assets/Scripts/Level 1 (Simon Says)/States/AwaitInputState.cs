using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    // the player's turn. each shot is checked against Sequence[step].
    // wrong target or out of time -> FailState.
    // whole sequence right -> RoundCompleteState (or SolvedState after the last round).
    public class AwaitInputState : ISimonState
    {
        private readonly SimonPuzzle puzzle;
        private int step;      // which shot in the sequence we're waiting for
        private float timer;   // time left for the next shot

        public AwaitInputState(SimonPuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public void Enter()
        {
            step = 0;
            timer = puzzle.Config.inputTimeout;
            puzzle.SetPrompt("YOUR TURN", puzzle.Config.inputColour);
            UpdateInfo();
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f)
            {
                puzzle.ChangeState(new FailState(puzzle, "TOO SLOW"));   // ran out of time
                return;
            }
            UpdateInfo();   // update the countdown on the screen
        }

        // the main check of the whole puzzle
        public void OnStationShot(int stationId)
        {
            // wrong target = fail
            if (stationId != puzzle.Sequence[step])
            {
                puzzle.ChangeState(new FailState(puzzle, "WRONG TARGET"));
                return;
            }

            // right target: blink, move to the next step, reset the timer
            puzzle.Station(stationId).Confirm();
            step++;
            timer = puzzle.Config.inputTimeout;           // fresh time for the next shot

            if (step < puzzle.Sequence.Count) return;     // still more to shoot

            // whole sequence done: was that the last round?
            bool lastRound = puzzle.Round + 1 >= puzzle.Config.rounds;
            if (lastRound) puzzle.ChangeState(new SolvedState(puzzle));
            else puzzle.ChangeState(new RoundCompleteState(puzzle));
        }

        public void OnStartShot() { }

        public void Exit() { }

        // shows round, progress and time left, e.g. "ROUND 1/3 | 2/3 | 3.4s"
        private void UpdateInfo()
        {
            puzzle.SetInfo($"{puzzle.RoundLabel}   |   {step}/{puzzle.Sequence.Count}   |   {Mathf.Max(0f, timer):0.0}s");
        }
    }
}
