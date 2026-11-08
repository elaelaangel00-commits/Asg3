using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip footstepLoopClip;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.5f;

    [Header("Movement Detection")]
    [SerializeField] private float minimumMoveSpeed = 0.1f;
    [SerializeField] private float stopDelay = 0.1f;

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
