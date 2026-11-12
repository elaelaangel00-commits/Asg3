using TMPro;
using UnityEngine;

// Updates the objective text when the puzzle is solved
public class Level2UIBridge : MonoBehaviour
{
    [SerializeField] private PressurePlatePuzzle puzzle;
    [SerializeField] private TMP_Text objectiveText;

    private void OnEnable()
    {
        if (puzzle != null)
        {
            puzzle.PuzzleSolved += HandleSolved;
        }
    }

    private void OnDisable()
    {
        if (puzzle != null)
        {
            puzzle.PuzzleSolved -= HandleSolved;
        }
    }

    private void Start()
    {
        if (puzzle != null && puzzle.IsSolved)
        {
            HandleSolved();
        }
    }

    private void HandleSolved()
    {
        if (objectiveText != null)
        {
            objectiveText.text = "ROOM 2 COMPLETE!\n" + "The door is now unlocked.";
        }
    }
}
