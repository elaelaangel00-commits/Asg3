using System.Collections;
using UnityEngine;

/// <summary>
/// The exit door of Room 5. It starts locked and swings open when Unlock() is called.
/// Setup: put this on an empty "DoorHinge" object placed at the door's hinge edge,
/// with the door model as a child of it.
/// </summary>
public class ExitDoor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float openAngle = 90f;      /// how far the door swings (use -90 to swing the other way)
    [SerializeField] private float openDuration = 1.5f;  /// seconds the swing takes

    [Header("References")]
    [SerializeField] private Collider blockingCollider; /// the solid collider stopping the player
    [SerializeField] private GameObject exitTrigger;    /// trigger behind the door, keep DISABLED
    [SerializeField] private AudioSource openSound;     /// played once when the door unlocks

    private bool isUnlocked;

    private void Start()
    {
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
        Quaternion start = transform.localRotation;
        Quaternion end = start * Quaternion.Euler(0f, openAngle, 0f);   /// rotate around the Y axis
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / openDuration;
            transform.localRotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }

        if (blockingCollider != null) blockingCollider.enabled = false;
        if (exitTrigger != null) exitTrigger.SetActive(true);
    }
}