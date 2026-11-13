namespace Backrooms.Puzzles.Simon
{
    // short pause between rounds. the sequence gets one step longer.
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
            puzzle.ExtendSequence();   // same sequence + 1 new step at the end
            puzzle.PlayRoundClear();
            timer = puzzle.Config.betweenRounds;

            puzzle.SetPrompt("ROUND CLEAR", puzzle.Config.successColour);
            puzzle.SetInfo($"NEXT: {puzzle.RoundLabel}");
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f) puzzle.ChangeState(new ShowSequenceState(puzzle));   // show the longer sequence
        }

        public void OnStationShot(int stationId) { }

        public void OnStartShot() { }

        public void Exit() { }
    }
}
