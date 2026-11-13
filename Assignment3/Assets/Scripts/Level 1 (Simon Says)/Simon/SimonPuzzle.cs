using System.Collections.Generic;
using Backrooms.Core;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    // the main simon says script. this is the CONTEXT of the state pattern.
    // it holds the data (sequence, round, stations) and small helper functions.
    // it does NOT hold the rules. the rules live in the state classes
    // (Idle, ShowSequence, AwaitInput, RoundComplete, Fail, Solved).
    // it inherits from Puzzle, so doors and ui treat it like any other puzzle.
    public class SimonPuzzle : Puzzle
    {
        [Header("Setup")]
        [SerializeField] private SimonConfig config;          // all the tuning numbers
        [SerializeField] private SimonStation[] stations;     // left to right, index = id
        [SerializeField] private ShootableButton startButton;
        [SerializeField] private SimonDisplay display;
        [SerializeField] private AudioSource puzzleAudio;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private KeyCode debugSolveKey = KeyCode.F9;   // editor only, skip the puzzle

        // public "read" access so the states can use the data
        public SimonConfig Config => config;
        public List<int> Sequence { get; } = new List<int>();   // list, because it grows every round
        public int Round { get; set; }                        // starts at 0
        public string RoundLabel => $"ROUND {Round + 1}/{config.rounds}";   // +1 so players see "round 1"

        private ISimonState current;   // the state we're in right now
        private bool solved;
        private AudioClip failClip;
        private AudioClip roundClip;
        private AudioClip solvedClip;

        // ---------- unity lifecycle ----------

        // subscribe to events: "tell me when a station or the start button is shot"
        private void OnEnable()
        {
            foreach (SimonStation station in stations) station.Shot += HandleStationShot;
            if (startButton != null) startButton.Shot += HandleStartShot;
        }

        // unsubscribe when disabled, so nothing calls into a removed object
        private void OnDisable()
        {
            foreach (SimonStation station in stations) station.Shot -= HandleStationShot;
            if (startButton != null) startButton.Shot -= HandleStartShot;
        }

        private void Start()
        {
            // no config = can't run, so turn off with a clear error instead of crashing later
            if (config == null)
            {
                Debug.LogError("SimonPuzzle: no SimonConfig assigned.", this);
                enabled = false;
                return;
            }

            // give each station its id, colour and note from the config
            for (int i = 0; i < stations.Length; i++)
            {
                AudioClip tone = ToneGenerator.CreateTone($"SimonTone_{i}", config.FrequencyFor(i), 0.45f);
                stations[i].Setup(i, config.ColourFor(i), tone);
            }

            // make the puzzle sounds in code
            failClip = ToneGenerator.CreateBuzz("SimonFail", 110f, 0.6f);
            roundClip = ToneGenerator.CreateArpeggio("SimonRound", new[] { 659.25f, 987.77f }, 0.12f);
            solvedClip = ToneGenerator.CreateArpeggio("SimonSolved", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.15f);

            ChangeState(new IdleState(this));   // first state: wait for START
        }

        private void Update()
        {
            // debug shortcut: skip straight to solved (only works in the editor)
            if (Application.isEditor && !solved && Input.GetKeyDown(debugSolveKey))
                ChangeState(new SolvedState(this));

            // let the current state do its work this frame.
            // deltaTime is 0 while paused, so the puzzle freezes too.
            current?.Tick(Time.deltaTime);
        }

        // ---------- state machine ----------

        // the only way to switch state: clean up the old one, then set up the new one
        public void ChangeState(ISimonState next)
        {
            current?.Exit();
            current = next;
            if (logStateChanges) Debug.Log($"[Simon] -> {next.GetType().Name}", this);
            current.Enter();
        }

        // pass shots straight to whatever state we're in. the state decides what it means.
        private void HandleStationShot(int id) => current?.OnStationShot(id);
        private void HandleStartShot() => current?.OnStartShot();

        // ---------- sequence ----------

        // back to round 1 with a brand new random sequence (used after a fail)
        public void ResetSequence()
        {
            Round = 0;
            Sequence.Clear();
            SequenceGenerator.Extend(Sequence, config.startLength, stations.Length);
        }

        // add steps to the END of the same sequence (classic simon: it grows each round)
        public void ExtendSequence()
        {
            SequenceGenerator.Extend(Sequence, config.stepsAddedPerRound, stations.Length);
        }

        // ---------- helpers the states use ----------

        public SimonStation Station(int id) => stations[id];

        public void SetAllLamps(bool lit)
        {
            foreach (SimonStation station in stations) station.SetLit(lit);
        }

        public void FlickerAllLamps()
        {
            foreach (SimonStation station in stations) station.FailFlicker(config.failColour);
        }

        public void SetStartButton(bool interactable)
        {
            if (startButton != null) startButton.SetInteractable(interactable);
        }

        public void SetPrompt(string message, Color colour)
        {
            if (display != null) display.SetPrompt(message, colour);
        }

        public void SetInfo(string message)
        {
            if (display != null) display.SetInfo(message);
        }

        public void PlayFail() => Play(failClip);
        public void PlayRoundClear() => Play(roundClip);
        public void PlaySolved() => Play(solvedClip);

        private void Play(AudioClip clip)
        {
            if (puzzleAudio != null && clip != null) puzzleAudio.PlayOneShot(clip);
        }

        // ---------- puzzle base class ----------

        // called by SolvedState
        public void MarkSolved()
        {
            solved = true;
            Evaluate();   // from Puzzle: fires the Solved event, so the exit door unlocks
        }

        // our rule for "am i solved?" (required by the Puzzle base class)
        protected override bool CheckConditions() => solved;
    }
}
