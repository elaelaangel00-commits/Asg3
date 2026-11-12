using UnityEngine;

// Manages the pressure plate puzzle
public class PressurePlatePuzzle : MonoBehaviour
{
    [SerializeField] private PressurePlate[] plates;

    [SerializeField] private AudioSource puzzleAudio;
    [SerializeField] private AudioClip puzzleSolvedClip;

    public bool IsSolved { get; private set; }

    public event System.Action PuzzleSolved;
    
    private void OnEnable()
    {
        if (plates == null)
        {
            return;
        }

        foreach (PressurePlate plate in plates)
        {
            if (plate != null)
            {
                plate.StateChanged += HandlePlateStateChanged;
            }
        }
    }

    private void OnDisable()
    {
        if (plates == null)
        {
            return;
        }

        foreach (PressurePlate plate in plates)
        {
            if (plate != null)
            {
                plate.StateChanged -= HandlePlateStateChanged;
            }
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

    // Checks whether each plate is occupied by one object of the correct weight
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

        bool solved = allOccupied && allPlateWeightsCorrect;

        // Updates the visual feedback on every plate
        foreach (PressurePlate plate in plates)
        {
            plate.SetFeedback(allOccupied, solved);
        }

        if (!solved)
        {
            return;
        }

        // Marks the puzzle as solved and notifies other scripts
        IsSolved = true;
        PuzzleSolved?.Invoke();

        if (puzzleAudio != null && puzzleSolvedClip != null)
        {
            puzzleAudio.PlayOneShot(puzzleSolvedClip);
        }
    }
}
