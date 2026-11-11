
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ExitDoorTrigger : MonoBehaviour
{
    [Header("Next Level")]
    public string nextSceneName = "Level 5";

    [Header("Player")]
    public Transform playerRoot;

    private bool unlocked = false;
    private bool loading = false;
    private bool sceneErrorLogged = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void Unlock()
    {
        if (unlocked)
            return;

        unlocked = true;

        Debug.Log(
            "[EXIT] Door unlocked! Player can enter."
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(
            "[EXIT] Trigger touched by: " +
            other.name
        );

        TryLoadLevel(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryLoadLevel(other);
    }

    private bool IsPlayer(Collider other)
    {
        // Match the Player assigned in Inspector.
        if (playerRoot != null)
        {
            return other.transform == playerRoot ||
                   other.transform.IsChildOf(playerRoot);
        }

        // Fallback if Player Root is not assigned.
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player") ||
               other.GetComponentInParent<CharacterController>()
                   != null;
    }

    private void TryLoadLevel(Collider other)
    {
        if (loading)
            return;

        if (!IsPlayer(other))
            return;

        if (!unlocked)
            return;

        if (!Application.CanStreamedLevelBeLoaded(
            nextSceneName))
        {
            if (!sceneErrorLogged)
            {
                Debug.LogError(
                    "[EXIT] Cannot load scene: " +
                    nextSceneName +
                    ". Add it to Build Profiles!"
                );

                sceneErrorLogged = true;
            }

            return;
        }

        loading = true;

        Debug.Log(
            "[EXIT] Player entered! Loading: " +
            nextSceneName
        );

        SceneManager.LoadScene(nextSceneName);
    }
}
