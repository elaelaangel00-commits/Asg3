using UnityEngine;

public class KeyAndExitHandler : MonoBehaviour
{
    public enum Role { Key, ExitDoor }

    [Header("Setup")]
    public Role role;
    public GameObject exitDoor;   // only needed when Role = ExitDoor

    // The player's interact script calls this
    public void Interact()
    {
        if (role == Role.Key) PickUpKey();
        else TryOpenExitDoor();
    }

    private void PickUpKey()
    {
        if (Level5Manager.Instance == null) return;

        if (Level5Manager.Instance.IsAllAngelsSecured())
        {
            Level5Manager.Instance.GiveKey();
            Debug.Log("Key acquired!");
            gameObject.SetActive(false);   // hide the key
        }
    }

    private void TryOpenExitDoor()
    {
        if (Level5Manager.Instance == null) return;

        if (Level5Manager.Instance.HasKey)
        {
            Debug.Log("Door unlocked!");
            if (exitDoor != null) exitDoor.SetActive(false);   // open the door
        }
        else
        {
            Debug.Log("The door is locked. You need the key (secure all angels first).");
        }
    }
}