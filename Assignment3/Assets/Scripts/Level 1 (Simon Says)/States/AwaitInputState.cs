using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// The player's turn. Each shot is checked against Sequence[step].
    /// Wrong target or running out of time -> FailState.
    /// Whole sequence correct -> RoundCompleteState, or SolvedState after the last round.
    /// </summary>
    public class AwaitInputState : ISimonState
    {
        private readonly SimonPuzzle puzzle;
        private int step;
        private float timer;

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
                puzzle.ChangeState(new FailState(puzzle, "TOO SLOW"));
                return;
            }
            UpdateInfo();
        }

        public void OnStationShot(int stationId)
        {
            if (stationId != puzzle.Sequence[step])
            {
                puzzle.ChangeState(new FailState(puzzle, "WRONG TARGET"));
                return;
            }

            puzzle.Station(stationId).Confirm();
            step++;
            timer = puzzle.Config.inputTimeout;           // fresh time for the next shot

            if (step < puzzle.Sequence.Count) return;     // still more to shoot

            bool lastRound = puzzle.Round + 1 >= puzzle.Config.rounds;
            if (lastRound) puzzle.ChangeState(new SolvedState(puzzle));
            else puzzle.ChangeState(new RoundCompleteState(puzzle));
        }

        public void OnStartShot() { }

        public void Exit() { }

        private void UpdateInfo()
        {
            puzzle.SetInfo($"{puzzle.RoundLabel}   |   {step}/{puzzle.Sequence.Count}   |   {Mathf.Max(0f, timer):0.0}s");
        }
    }
}
