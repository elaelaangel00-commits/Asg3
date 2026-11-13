namespace Backrooms.Puzzles.Simon
{
    // final state. all lamps on, success sound, and the puzzle tells everyone it's solved.
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
            puzzle.MarkSolved();   // fires the Solved event -> exit door unlocks
        }

        // nothing else happens after solving
        public void Tick(float deltaTime) { }

        public void OnStationShot(int stationId) { }

        public void OnStartShot() { }

        public void Exit() { }
    }
}
