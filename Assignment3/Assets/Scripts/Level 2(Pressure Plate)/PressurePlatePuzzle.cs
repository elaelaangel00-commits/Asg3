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

        foreach (PressurePlateRule rule in rules)
        {
            if (rule == null)
            {
                continue;
            }

            if (!rule.IsValid())
            {
                statusUI?.ShowMessage(rule.GetFailureMessage());
                return;
            }
        }

        IsSolved = true;

        statusUI?.ShowMessage("Pressure plate puzzle solved.");
        PuzzleSolved?.Invoke();
    }
}
