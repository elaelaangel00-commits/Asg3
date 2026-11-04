using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && Level5Manager.Instance != null)
            Level5Manager.Instance.CompleteLevel();
    }
}