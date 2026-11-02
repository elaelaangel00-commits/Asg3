using UnityEngine;

public class HoldingCellTrigger : MonoBehaviour
{
    [SerializeField] private Transform lockPoint;
    [SerializeField] private GameObject cellGate;   // optional, closes after capture
    [SerializeField] private AudioSource lockSound;

    private bool occupied;
    private void OnTriggerEnter(Collider other)
    {
        if (occupied) return;

        WeepingAngelAI angel = other.GetComponentInParent<WeepingAngelAI>();
        if (angel == null || angel.IsLocked) return;

        occupied = true;
        angel.LockInCell(lockPoint);
        if (cellGate != null) cellGate.SetActive(true);
        if (lockSound != null) lockSound.Play();
    }
}