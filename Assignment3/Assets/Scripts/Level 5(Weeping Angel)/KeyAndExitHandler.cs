using UnityEngine;

public class KeyAndExitHandler : MonoBehaviour
{
    [Header("References")]
    public GameObject exitDoor; // Reference to your final exit door object

    private bool hasKey = false;

    // Call this when the player interacts with the spawned key object
    public void PickUpKey()
    {
        if (Level5Manager.Instance != null && Level5Manager.Instance.IsAllAngelsSecured())
        {
            hasKey = true;
            Debug.Log("Key acquired!");
            gameObject.SetActive(false); // Hide key object after pickup
        }
    }

    // Call this when the player tries to open the exit door
    public void TryOpenExitDoor()
    {
        if (hasKey)
        {
            Debug.Log("Door unlocked! Escaped Level 5.");
            if (Level5Manager.Instance != null)
            {
                Level5Manager.Instance.CompleteLevel();
            }
        }
        else
        {
            Debug.Log("The door is locked. You need the key from the box (secure all angels first).");
        }
    }
}