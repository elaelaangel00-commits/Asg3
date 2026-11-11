using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        pressurePlatePuzzle.PuzzleSolved += OnPuzzleSolved;
    }

    private void OnDisable()
    {
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
