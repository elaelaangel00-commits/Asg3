using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    /// <summary>Waiting for the player to shoot START. Targets do nothing yet.</summary>
    public class IdleState : ISimonState
    {
        private readonly SimonPuzzle puzzle;

        public IdleState(SimonPuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public void Enter()
        {
            puzzle.SetAllLamps(false);
            puzzle.SetStartButton(true);
            puzzle.SetPrompt("SHOOT START", Color.white);
            puzzle.SetInfo(puzzle.RoundLabel);
        }

        public void Tick(float deltaTime) { }

        public void OnStationShot(int stationId) { }      // ignored until the game starts

        public void OnStartShot()
        {
            if (puzzle.Sequence.Count == 0) puzzle.ResetSequence();
            puzzle.ChangeState(new ShowSequenceState(puzzle));
        }

        public void Exit()
        {
            puzzle.SetStartButton(false);
        }
    }
}
