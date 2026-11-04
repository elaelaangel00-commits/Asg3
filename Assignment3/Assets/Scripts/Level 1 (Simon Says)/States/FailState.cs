namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// Wrong target or too slow. Buzz, red flicker, and a brand-new sequence from round 1.
    /// After a short delay the flashing restarts by itself
    /// (or waits for START again if restartAutomatically is off in the config).
    /// </summary>
    public class FailState : ISimonState
    {
        private readonly SimonPuzzle puzzle;
        private readonly string reason;
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
            puzzle.ResetSequence();
            timer = puzzle.Config.failDelay;

            puzzle.SetPrompt(reason, puzzle.Config.failColour);
            puzzle.SetInfo(puzzle.Config.restartAutomatically ? "RESTARTING..." : "SHOOT START TO RETRY");
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return;

            if (puzzle.Config.restartAutomatically) puzzle.ChangeState(new ShowSequenceState(puzzle));
            else puzzle.ChangeState(new IdleState(puzzle));
        }

        public void OnStationShot(int stationId) { }

        public void OnStartShot() { }

        public void Exit() { }
    }
}
