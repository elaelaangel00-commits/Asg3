using System.Collections.Generic;
using Backrooms.Core;
using UnityEngine;

namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// The CONTEXT of the State pattern.
    /// It owns the data (sequence, round, stations) and helper actions.
    /// All the rules (what happens when a target is shot, when to flash, when to fail)
    /// live in the state classes, never here.
    /// </summary>
    public class SimonPuzzle : Puzzle
    {
        [Header("Setup")]
        [SerializeField] private SimonConfig config;
        [SerializeField] private SimonStation[] stations;     // left to right
        [SerializeField] private ShootableButton startButton;
        [SerializeField] private SimonDisplay display;
        [SerializeField] private AudioSource puzzleAudio;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private KeyCode debugSolveKey = KeyCode.F9;   // editor only

        public SimonConfig Config => config;
        public List<int> Sequence { get; } = new List<int>();
        public int Round { get; set; }                        // 0-based
        public string RoundLabel => $"ROUND {Round + 1}/{config.rounds}";

        private ISimonState current;
        private bool solved;
        private AudioClip failClip;
        private AudioClip roundClip;
        private AudioClip solvedClip;

        // ---------- Unity lifecycle ----------

        private void OnEnable()
        {
            foreach (SimonStation station in stations) station.Shot += HandleStationShot;
            if (startButton != null) startButton.Shot += HandleStartShot;
        }

        private void OnDisable()
        {
            foreach (SimonStation station in stations) station.Shot -= HandleStationShot;
            if (startButton != null) startButton.Shot -= HandleStartShot;
        }

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("SimonPuzzle: no SimonConfig assigned.", this);
                enabled = false;
                return;
            }

            // Give every station its id, colour and tone from the config
            for (int i = 0; i < stations.Length; i++)
            {
                AudioClip tone = ToneGenerator.CreateTone($"SimonTone_{i}", config.FrequencyFor(i), 0.45f);
                stations[i].Setup(i, config.ColourFor(i), tone);
            }

            failClip = ToneGenerator.CreateBuzz("SimonFail", 110f, 0.6f);
            roundClip = ToneGenerator.CreateArpeggio("SimonRound", new[] { 659.25f, 987.77f }, 0.12f);
            solvedClip = ToneGenerator.CreateArpeggio("SimonSolved", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.15f);

            ChangeState(new IdleState(this));
        }

        private void Update()
        {
            if (Application.isEditor && !solved && Input.GetKeyDown(debugSolveKey))
                ChangeState(new SolvedState(this));

            current?.Tick(Time.deltaTime);      // Time.deltaTime is 0 while paused, so the puzzle freezes too
        }

        // ---------- State machine ----------

        public void ChangeState(ISimonState next)
        {
            current?.Exit();
            current = next;
            if (logStateChanges) Debug.Log($"[Simon] -> {next.GetType().Name}", this);
            current.Enter();
        }

        private void HandleStationShot(int id) => current?.OnStationShot(id);
        private void HandleStartShot() => current?.OnStartShot();

        // ---------- Sequence ----------

        /// <summary>Back to round 1 with a brand-new random sequence.</summary>
        public void ResetSequence()
        {
            Round = 0;
            Sequence.Clear();
            SequenceGenerator.Extend(Sequence, config.startLength, stations.Length);
        }

        /// <summary>Adds steps to the END of the current sequence (classic Simon).</summary>
        public void ExtendSequence()
        {
            SequenceGenerator.Extend(Sequence, config.stepsAddedPerRound, stations.Length);
        }

        // ---------- Helpers the states use ----------

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

        // ---------- Puzzle base class ----------

        public void MarkSolved()
        {
            solved = true;
            Evaluate();                         // fires Solved -> door opens, PuzzleManager counts it
        }

        protected override bool CheckConditions() => solved;
    }
}
