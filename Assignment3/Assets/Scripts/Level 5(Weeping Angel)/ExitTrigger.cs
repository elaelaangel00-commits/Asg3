using UnityEngine;

// Put this on a trigger collider just past the door. ExitDoor enables it after opening.
public class ExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (Level5Manager.Instance != null)
            Level5Manager.Instance.CompleteLevel();
    }
}