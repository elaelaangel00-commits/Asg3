
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ExitDoorTrigger : MonoBehaviour
{
    [Header("Next Level")]
    public string nextSceneName = "Level 5";

    [Header("Player")]
    public Transform playerRoot;

    // The door starts locked.
    private bool unlocked = false;

    // Prevent loading the scene more than once.
    private bool loading = false;


    private void Awake()
    {
        // Player can enter the trigger zone.
        GetComponent<Collider>().isTrigger = true;
    }


    // Called by LaserPuzzle when solved.
    public void Unlock()
    {
        if (unlocked)
            return;

        unlocked = true;

        Debug.Log("Exit door unlocked!");
    }


    // Runs when something enters the trigger.
    private void OnTriggerEnter(Collider other)
    {
        TryLoadLevel(other);
    }


    // Also checks players already in the trigger.
    private void OnTriggerStay(Collider other)
    {
        TryLoadLevel(other);
    }


    // Check whether this object is the player.
    private bool IsPlayer(Collider other)
    {
        if (playerRoot != null)
        {
            return other.transform == playerRoot ||
                   other.transform.IsChildOf(playerRoot);
        }

        // Fallback when Player Root is not assigned.
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player") ||
               other.GetComponentInParent<CharacterController>()
                   != null;
    }


    // Load the next level if door is unlocked.
    private void TryLoadLevel(Collider other)
    {
        // Prevent repeated scene loading.
        if (loading)
            return;

        // Only allow the player.
        if (!IsPlayer(other))
            return;

        // Do not enter if puzzle is unsolved.
        if (!unlocked)
            return;

        // Check if the next scene is in the build.
        if (!Application.CanStreamedLevelBeLoaded(
            nextSceneName))
        {
            Debug.LogError(
                "Scene not found: " + nextSceneName +
                ". Add it to Build Profiles!"
            );

            return;
        }

        loading = true;

        Debug.Log("Loading " + nextSceneName);

        // Change to the next scene.
        SceneManager.LoadScene(nextSceneName);
    }
}
