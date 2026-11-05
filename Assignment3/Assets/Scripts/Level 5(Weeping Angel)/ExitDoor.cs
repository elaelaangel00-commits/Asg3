using System.Collections;
using UnityEngine;

// Put this on the exit door. It starts locked and slides open when Unlock() is called.
public class ExitDoor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f); // slide up 3 units
    [SerializeField] private float openDuration = 1.5f;

    [Header("References")]
    [SerializeField] private Collider blockingCollider; // the solid collider stopping the player
    [SerializeField] private GameObject exitTrigger;    // trigger behind the door (ExitTrigger.cs), keep DISABLED
    [SerializeField] private AudioSource openSound;

    private bool isUnlocked;

    private void Start()
    {
        // Make sure the exit trigger starts off
        if (exitTrigger != null) exitTrigger.SetActive(false);
    }

    public void Unlock()
    {
        if (isUnlocked) return;
        isUnlocked = true;

        if (openSound != null) openSound.Play();
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end = start + openOffset;
        float t = 0f;

        // Smoothly slide the door from closed to open
        while (t < 1f)
        {
            t += Time.deltaTime / openDuration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        // Door is open: let the player through and activate the exit
        if (blockingCollider != null) blockingCollider.enabled = false;
        if (exitTrigger != null) exitTrigger.SetActive(true);
    }
}