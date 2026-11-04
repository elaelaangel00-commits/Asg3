namespace Backrooms.Puzzles.Simon
{
    /// <summary>Final state. All lamps on, success sound, and the puzzle reports itself solved.</summary>
    public class SolvedState : ISimonState
    {
        private readonly SimonPuzzle puzzle;

        public SolvedState(SimonPuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public void Enter()
        {
            puzzle.SetAllLamps(true);
            puzzle.SetStartButton(false);
            puzzle.PlaySolved();
            puzzle.SetPrompt("ACCESS GRANTED", puzzle.Config.successColour);
            puzzle.SetInfo("THE DOOR IS OPEN");
            puzzle.MarkSolved();
        }

        public void Tick(float deltaTime) { }

        public void OnStationShot(int stationId) { }

        public void OnStartShot() { }

        public void Exit() { }
    }
}
