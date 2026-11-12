using UnityEngine;
using UnityEngine.SceneManagement;

// Controls the level exit door - the door only becomes interactive after the puzzle has been solved
public class DoorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PressurePlatePuzzle pressurePlatePuzzle;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField, Min(0f)] private float loadDelay = 0.5f;

    [Header("Next Level")]
    [SerializeField] private string nextSceneName = "Level 3";

    [Header("Audio")]
    [SerializeField] private AudioSource doorOpenAudio;

    private bool puzzleSolved;
    private bool hasStartedLoading;

    public bool CanInteract => puzzleSolved && !hasStartedLoading;

    private void OnEnable()
    {
        if (pressurePlatePuzzle == null)
        {
            return;
        }

        pressurePlatePuzzle.PuzzleSolved += OnPuzzleSolved;
    }

    private void OnDisable()
    {
        if (pressurePlatePuzzle == null)
        {
            return;
        }

        pressurePlatePuzzle.PuzzleSolved -= OnPuzzleSolved;
    }

    private void OnPuzzleSolved()
    {
        puzzleSolved = true;
    }

    private void Update()
    {
        if (!CanInteract)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            StartCoroutine(LoadNextLevel());
        }
    }

    // Checks whether a Collider belongs to the door or one of its child objects
    // Used by InteractionPromptUI to determine whether the player is looking at the door
    public bool ContainsCollider(Collider collider)
    {
        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    private System.Collections.IEnumerator LoadNextLevel()
    {
        hasStartedLoading = true;

        if (doorOpenAudio != null)
        {
            doorOpenAudio.Play();
        }

        yield return new WaitForSeconds(loadDelay);

        SceneManager.LoadScene(nextSceneName);
    }
}
