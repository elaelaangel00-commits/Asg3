using UnityEngine;

// Plays looping footsteps audio whenever the player moves
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement; // Used to heck whether the player is grounded
    [SerializeField] private Rigidbody playerRigidbody; // Used to measure the player's movement speed

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip footstepLoopClip;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.5f;

    [Header("Movement Detection")]
    [SerializeField] private float minimumMoveSpeed = 0.1f; // Minimum horizontal speed required for audio to play
    [SerializeField] private float stopDelay = 0.1f; // Delay audio stopping after movement stops

    private AudioSource footstepAudio;
    private bool isWalking;
    private float stopTimer;

    private void Awake()
    {
        footstepAudio = GetComponent<AudioSource>();

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        if (footstepLoopClip != null)
        {
            footstepAudio.clip = footstepLoopClip;
        }

        footstepAudio.loop = true;
        footstepAudio.playOnAwake = false;
        footstepAudio.spatialBlend = 0f;
        footstepAudio.volume = footstepVolume;
    }

    private void Update()
    {
        if (playerMovement == null || playerRigidbody == null)
        {
            return;
        }

        // Removes vertical movement so that jumping and falling do not trigger the audio
        Vector3 horizontalVelocity = playerRigidbody.linearVelocity;
        horizontalVelocity.y = 0f;

        bool shouldBeWalking = playerMovement.IsGrounded && horizontalVelocity.magnitude > minimumMoveSpeed;

        if (shouldBeWalking)
        {
            stopTimer = 0f;

            if (!isWalking)
            {
                StartWalking();
            }
        }
        else if (isWalking)
        {
            stopTimer += Time.deltaTime;

            if (stopTimer >= stopDelay)
            {
                StopWalking();
            }
        }
    }

    private void StartWalking()
    {
        isWalking = true;

        if (!footstepAudio.isPlaying)
        {
            footstepAudio.Play();
        }
    }

    private void StopWalking()
    {
        isWalking = false;

        footstepAudio.Stop();
    }
}
