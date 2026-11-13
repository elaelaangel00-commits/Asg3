namespace Backrooms.Puzzles.Simon
{
    // plays the sequence: lamp on -> wait -> lamp off -> wait gap -> next lamp.
    // uses a timer instead of a coroutine, so pausing freezes it,
    // and leaving the state can't leave anything running in the background.
    // shots are ignored here: OnStationShot is empty, that's how input is "locked".
    public class ShowSequenceState : ISimonState
    {
        private readonly SimonPuzzle puzzle;
        private int index;      // which step of the sequence we're showing
        private float timer;    // counts down to the next change
        private bool lampOn;    // is the current lamp lit right now?

        public ShowSequenceState(SimonPuzzle puzzle)
        {
            this.puzzle = puzzle;
        }

        public void Enter()
        {
            index = 0;
            lampOn = false;
            timer = puzzle.Config.leadIn;   // short pause before the first flash

            puzzle.SetAllLamps(false);
            puzzle.SetPrompt("WATCH", puzzle.Config.watchColour);
            puzzle.SetInfo($"{puzzle.RoundLabel}   |   {puzzle.Sequence.Count} FLASHES");
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return;   // not time yet

            if (lampOn)
            {
                // lamp has been on long enough: turn it off and wait the gap
                puzzle.Station(puzzle.Sequence[index]).SetLit(false);
                lampOn = false;
                index++;
                timer = puzzle.Config.GapFor(puzzle.Round);
            }
            else if (index < puzzle.Sequence.Count)
            {
                // gap is over: light the next lamp (with its note)
                puzzle.Station(puzzle.Sequence[index]).Flash();
                lampOn = true;
                timer = puzzle.Config.FlashFor(puzzle.Round);   // faster in later rounds
            }
            else
            {
                // every lamp has been shown: player's turn
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
