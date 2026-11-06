namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// Plays the sequence: lamp on -> wait -> lamp off -> wait gap -> next lamp.
    /// Uses a timer instead of a coroutine, so pausing freezes it and leaving the state
    /// cannot leave anything running in the background.
    /// Shots are ignored here: input is locked by simply doing nothing in OnStationShot.
    /// </summary>
    public class ShowSequenceState : ISimonState
    {
        private readonly SimonPuzzle puzzle;
        private int index;
        private float timer;
        private bool lampOn;

        public ShowSequenceState(SimonPuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public void Enter()
        {
            index = 0;
            lampOn = false;
            timer = puzzle.Config.leadIn;

            puzzle.SetAllLamps(false);
            puzzle.SetPrompt("WATCH", puzzle.Config.watchColour);
            puzzle.SetInfo($"{puzzle.RoundLabel}   |   {puzzle.Sequence.Count} FLASHES");
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return;

            if (lampOn)
            {
                // Current lamp has been on long enough: turn it off and wait the gap
                puzzle.Station(puzzle.Sequence[index]).SetLit(false);
                lampOn = false;
                index++;
                timer = puzzle.Config.GapFor(puzzle.Round);
            }
            else if (index < puzzle.Sequence.Count)
            {
                // Gap is over: light the next lamp in the sequence
                puzzle.Station(puzzle.Sequence[index]).Flash();
                lampOn = true;
                timer = puzzle.Config.FlashFor(puzzle.Round);
            }
            else
            {
                // Every lamp has been shown
                puzzle.ChangeState(new AwaitInputState(puzzle));
            }
        }

        public void OnStationShot(int stationId) { }     // input locked while watching

        public void OnStartShot() { }

        public void Exit()
        {
            puzzle.SetAllLamps(false);
        }
    }
}
