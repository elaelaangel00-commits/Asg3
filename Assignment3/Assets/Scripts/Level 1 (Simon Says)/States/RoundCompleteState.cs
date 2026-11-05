namespace Backrooms.Puzzles.Simon
{
    /// <summary>Short pause between rounds. Makes the sequence one step longer.</summary>
    public class RoundCompleteState : ISimonState
    {
        private readonly SimonPuzzle puzzle;
        private float timer;

        public RoundCompleteState(SimonPuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public void Enter()
        {
            puzzle.Round++;
            puzzle.ExtendSequence();
            puzzle.PlayRoundClear();
            timer = puzzle.Config.betweenRounds;

            puzzle.SetPrompt("ROUND CLEAR", puzzle.Config.successColour);
            puzzle.SetInfo($"NEXT: {puzzle.RoundLabel}");
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f) puzzle.ChangeState(new ShowSequenceState(puzzle));
        }

        public void OnStationShot(int stationId) { }

        public void OnStartShot() { }

        public void Exit() { }
    }
}
