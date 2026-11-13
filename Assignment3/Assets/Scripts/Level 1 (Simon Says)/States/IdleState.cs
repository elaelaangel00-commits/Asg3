using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    // waiting for the player to shoot START. shooting targets does nothing yet.
    public class IdleState : ISimonState
    {
        private readonly SimonPuzzle puzzle;   // the context, so this state can use its data

        public IdleState(SimonPuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public void Enter()
        {
            puzzle.SetAllLamps(false);
            puzzle.SetStartButton(true);   // start button glows
            puzzle.SetPrompt("SHOOT START", Color.white);
            puzzle.SetInfo(puzzle.RoundLabel);
        }

        public void Tick(float deltaTime) { }   // nothing to do every frame

        public void OnStationShot(int stationId) { }      // ignored until the game starts

        public void OnStartShot()
        {
            if (puzzle.Sequence.Count == 0) puzzle.ResetSequence();   // make a sequence if we don't have one
            puzzle.ChangeState(new ShowSequenceState(puzzle));
        }

        public void Exit()
        {
            puzzle.SetStartButton(false);   // lock the start button while playing
        }
    }
}
