using System.Collections;
using UnityEngine;

/// <summary>
/// The exit door of Room 5. It starts locked and slides open when Unlock() is called.
///
/// Where it fits in the puzzle flow:
///  1. The player places the skulls in the correct order.
///  2. TableDeposit calls exitDoor.Unlock().
///  3. The door slides up (OpenRoutine), then the solid collider is turned off
///     and the exit trigger behind the door is switched on.
///  4. The player walks through the open door and finishes the level.
///
/// Setup: put this on the exit door object.
/// </summary>
public class ExitDoor : MonoBehaviour
{
    
    /// Inspector settings
    
    [Header("Movement")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f); /// slide up 3 units
    [SerializeField] private float openDuration = 1.5f;                    /// seconds the slide takes

    [Header("References")]
    [SerializeField] private Collider blockingCollider; /// the solid collider stopping the player
    [SerializeField] private GameObject exitTrigger;    /// trigger behind the door (ExitTrigger.cs), keep DISABLED
    [SerializeField] private AudioSource openSound;     /// played once when the door starts opening

   
    /// Private state
 

    private bool isUnlocked;   /// stops Unlock() from running twice

    
    /// Unity lifecycle


    private void Start()
    {
        /// Make sure the exit trigger starts off, so the player can't finish the level
        /// by reaching the area behind the door before the puzzle is solved
        if (exitTrigger != null) exitTrigger.SetActive(false);
    }

    
    /// Called by other scripts
    /// Called by TableDeposit when the puzzle is solved
    public void Unlock()
    {
        if (isUnlocked) return;   /// guard: ignore repeated calls
        isUnlocked = true;

        if (openSound != null) openSound.Play();
        StartCoroutine(OpenRoutine());   /// slide the door open over time
    }

    
    /// Door animation
    /// Coroutine = a function that can pause between frames, which is how we animate over time
    private IEnumerator OpenRoutine()
    {
        Vector3 start = transform.position;      /// where the door is now (closed)
        Vector3 end = start + openOffset;        /// where it should end up (open)
        float t = 0f;                            /// progress: 0 = closed, 1 = fully open

        /// Smoothly slide the door from closed to open
        while (t < 1f)
        {
            /// Time.deltaTime = seconds since last frame, so dividing by openDuration
            /// makes t reach 1 after exactly openDuration seconds, at any frame rate
            t += Time.deltaTime / openDuration;

            /// Lerp = blend between two positions. t = 0 gives start, t = 1 gives end
            /// (Lerp clamps t, so the door never overshoots)
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;   /// wait for the next frame
        }

        /// Door is open: let the player through and activate the exit
        if (blockingCollider != null) blockingCollider.enabled = false;
        if (exitTrigger != null) exitTrigger.SetActive(true);
    }
}