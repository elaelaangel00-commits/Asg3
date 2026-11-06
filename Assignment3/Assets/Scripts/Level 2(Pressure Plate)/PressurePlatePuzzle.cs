using UnityEngine;

public class PressurePlatePuzzle : MonoBehaviour
{
    [SerializeField] private PressurePlate[] plates;
    [SerializeField] private PuzzleStatusUI statusUI;

    private PressurePlateRule[] rules;

    public bool IsSolved { get; private set; }

    public event System.Action PuzzleSolved;

    private void Awake()
    {
        rules = GetComponents<PressurePlateRule>();
    }
    
    private void OnEnable()
    {
        foreach (PressurePlate plate in plates)
        {
            plate.StateChanged += HandlePlateStateChanged;
        }
    }

    private void OnDisable()
    {
        foreach (PressurePlate plate in plates)
        {
            plate.StateChanged -= HandlePlateStateChanged;
        }
    }

    private void Start()
    {
        EvaluatePuzzle();
    }

    private void HandlePlateStateChanged(PressurePlate changedPlate)
    {
        EvaluatePuzzle();
    }

    private void EvaluatePuzzle()
    {
        if (IsSolved)
        {
            return;
        }

        if (plates == null || plates.Length == 0)
        {
            return;
        }

        bool allOccupied = true;
        bool allPlateWeightsCorrect = true;

        foreach (PressurePlate plate in plates)
        {
            if (plate == null)
            {
                return;
            }

            if (!plate.IsOccupied)
            {
                allOccupied = false;
            }

            if (!plate.HasCorrectWeight)
            {
                allPlateWeightsCorrect = false;
            }
        }

        bool allRulesPassed = true;

        foreach (PressurePlateRule rule in rules)
        {
            if (rule == null || !rule.isActiveAndEnabled)
            {
                continue;
            }

            if (!rule.IsValid())
            {
                allRulesPassed = false;
                break;
            }
        }

        bool solved = allOccupied && allPlateWeightsCorrect && allRulesPassed;

        foreach (PressurePlate plate in plates)
        {
            plate.SetFeedback(allOccupied, solved);
        }

        if (!solved)
        {
            return;
        }

        IsSolved = true;
        PuzzleSolved?.Invoke();
    }
}
