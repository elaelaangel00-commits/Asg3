namespace Backrooms.Puzzles.Simon
{
    // wrong target or too slow.
    // buzz, red flicker, and a brand new sequence from round 1.
    // after a short wait the flashing starts again by itself
    // (or waits for START if restartAutomatically is off in the config).
    public class FailState : ISimonState
    {
        private readonly SimonPuzzle puzzle;
        private readonly string reason;   // "WRONG TARGET" or "TOO SLOW"
        private float timer;

        public FailState(SimonPuzzle puzzle, string reason)
        {
            this.puzzle = puzzle;
            this.reason = reason;
        }

        public void Enter()
        {
            puzzle.PlayFail();
            puzzle.FlickerAllLamps();
            puzzle.ResetSequence();   // new random sequence, so the player can't just memorise it
            timer = puzzle.Config.failDelay;

            puzzle.SetPrompt(reason, puzzle.Config.failColour);
            puzzle.SetInfo(puzzle.Config.restartAutomatically ? "RESTARTING..." : "SHOOT START TO RETRY");
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return;

            // after the wait: restart straight away, or go back to waiting for START
            if (puzzle.Config.restartAutomatically) puzzle.ChangeState(new ShowSequenceState(puzzle));
            else puzzle.ChangeState(new IdleState(puzzle));
        }

        public void OnStationShot(int stationId) { }

        public void OnStartShot() { }

        public void Exit() { }
    }
}
