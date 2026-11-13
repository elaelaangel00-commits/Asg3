
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Automatically requires an AudioSource component.
[RequireComponent(typeof(AudioSource))]

public class PlayerInteract : MonoBehaviour
{
    // ==========================================
    // RAYCAST SETTINGS
    // ==========================================

    [Header("Raycast Settings")]

    // Maximum distance the player can interact.
    public float interactDistance = 3f;

    // Camera used to detect buttons.
    public Camera viewCamera;

    // TRUE = aim using mouse cursor.
    // FALSE = aim using centre crosshair.
    public bool useMouseHover = true;


    // ==========================================
    // INTERACTION SETTINGS
    // ==========================================

    [Header("Interaction")]

    // Enable to allow pressing E to interact.
    public bool allowEKey = true;


    // ==========================================
    // INTERACTION UI
    // ==========================================

    [Header("Interaction UI")]

    // Text displayed when pointing at a button.
    public TMP_Text interactionPrompt;

    // Enable only for a World Space Canvas.
    public bool movePromptAboveButton = false;

    // Height of the prompt above the button.
    public float promptHeight = 0.7f;


    // ==========================================
    // BUTTON SOUND EFFECT
    // ==========================================

    [Header("Button Click Sound")]

    // Drag your interaction sound here.
    public AudioClip selectionSound;

    // 0 = silent, 1 = full volume.
    [Range(0f, 1f)]
    public float selectionVolume = 1f;

    // AudioSource used to play sounds.
    private AudioSource audioSource;


    // ==========================================
    // AWAKE
    // ==========================================

    private void Awake()
    {
        // Get the AudioSource component.
        audioSource = GetComponent<AudioSource>();

        // Prevent sound playing automatically.
        audioSource.playOnAwake = false;

        // Use 2D audio so distance does not
        // affect the interaction sound.
        audioSource.spatialBlend = 0f;
    }


    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        // Find the camera if not assigned.
        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        // Otherwise, use the Main Camera.
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        // Hide interaction text initially.
        HidePrompt();
    }


    // ==========================================
    // UPDATE
    // ==========================================

    // Update runs once every frame.
    private void Update()
    {
        // Search for the button being pointed at.
        MirrorButton mirrorButton;
        LaserButton laserButton;

        bool foundButton = FindButton(
            out mirrorButton,
            out laserButton
        );


        // If no button is detected,
        // hide the interaction prompt.
        if (!foundButton)
        {
            HidePrompt();
            return;
        }


        // ======================================
        // SHOW INTERACTION TEXT
        // ======================================

        if (mirrorButton != null)
        {
            // Show mirror button instructions.
            ShowPrompt(
                mirrorButton.PromptText,
                mirrorButton.transform
            );
        }
        else if (laserButton != null)
        {
            // Show laser button instructions.
            ShowPrompt(
                laserButton.PromptText,
                laserButton.transform
            );
        }


        // ======================================
        // CHECK PLAYER INPUT
        // ======================================

        // Check for LEFT MOUSE CLICK.
        //
        // wasPressedThisFrame means the input
        // was pressed during the current frame.
        bool mouseClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;


        // Check whether the E KEY was pressed.
        //
        // allowEKey must be enabled.
        bool ePressed =
            allowEKey &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;


        // ======================================
        // WAIT FOR INTERACTION
        // ======================================

        // If the player has NOT left-clicked
        // and has NOT pressed E, do nothing.
        //
        // Hovering alone will NOT play sound.
        if (!mouseClicked && !ePressed)
        {
            return;
        }


        // ======================================
        // ACTIVATE MIRROR OR LASER BUTTON
        // ======================================

        if (mirrorButton != null)
        {
            // Check if a mirror is assigned.
            if (mirrorButton.mirror == null)
            {
                Debug.LogWarning(
                    "Mirror is not assigned!"
                );

                return;
            }

            // Rotate the mirror by 45 degrees.
            mirrorButton.Interact();
        }
        else if (laserButton != null)
        {
            // Check if LaserPuzzle is assigned.
            if (laserButton.laserPuzzle == null)
            {
                Debug.LogWarning(
                    "LaserPuzzle is not assigned!"
                );

                return;
            }

            // Turn laser ON or OFF.
            laserButton.Interact();
        }
        else
        {
            return;
        }


        // ======================================
        // PLAY INTERACTION SOUND
        // ======================================

        // Play the sound after a valid interaction.
        //
        // Works for BOTH:
        // - Left mouse click
        // - E key
        //
        // Called only once even if both inputs
        // are pressed during the same frame.
        PlayClickSound();


        // ======================================
        // REFRESH INTERACTION TEXT
        // ======================================

        // Update the prompt after interaction.
        //
        // Example:
        // "Turn laser ON" -> "Turn laser OFF".

        if (mirrorButton != null)
        {
            ShowPrompt(
                mirrorButton.PromptText,
                mirrorButton.transform
            );
        }
        else if (laserButton != null)
        {
            ShowPrompt(
                laserButton.PromptText,
                laserButton.transform
            );
        }
    }


    // ==========================================
    // FIND BUTTON USING RAYCAST
    // ==========================================

    private bool FindButton(
        out MirrorButton mirrorButton,
        out LaserButton laserButton)
    {
        // Start with no detected buttons.
        mirrorButton = null;
        laserButton = null;


        // A camera is required for raycasting.
        if (viewCamera == null)
        {
            return false;
        }


        // Ray = invisible line used to detect
        // objects with Colliders.
        Ray ray;


        // OPTION 1: Use the mouse cursor.
        if (useMouseHover && Mouse.current != null)
        {
            ray = viewCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );
        }
        else
        {
            // OPTION 2: Use the screen centre.
            ray = viewCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );
        }


        // Shoot the ray and detect a Collider.
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide))
        {
            // ----------------------------------
            // CHECK MIRROR BUTTON
            // ----------------------------------

            // Search for MirrorButton on the
            // detected object or its parent.
            mirrorButton = hit.collider
                .GetComponentInParent<MirrorButton>();

            if (mirrorButton != null)
            {
                return true;
            }


            // ----------------------------------
            // CHECK LASER BUTTON
            // ----------------------------------

            // If no mirror button exists,
            // search for a LaserButton.
            laserButton = hit.collider
                .GetComponentInParent<LaserButton>();

            if (laserButton != null)
            {
                return true;
            }
        }


        // No valid button was detected.
        return false;
    }


    // ==========================================
    // PLAY BUTTON SOUND
    // ==========================================

    private void PlayClickSound()
    {
        // Don't play if no sound is assigned.
        if (selectionSound == null)
        {
            return;
        }

        // PlayOneShot plays the clip once.
        //
        // It does not continuously loop.
        audioSource.PlayOneShot(
            selectionSound,
            selectionVolume
        );
    }


    // ==========================================
    // SHOW INTERACTION PROMPT
    // ==========================================

    private void ShowPrompt(
        string promptText,
        Transform buttonTransform)
    {
        // Check if TextMeshPro is assigned.
        if (interactionPrompt == null)
        {
            return;
        }

        // Make the interaction text visible.
        interactionPrompt.gameObject.SetActive(true);

        // Display the button instructions.
        interactionPrompt.text = promptText;


        // OPTIONAL:
        // Position text above the button.
        // Only for World Space Canvas.
        if (movePromptAboveButton &&
            buttonTransform != null)
        {
            interactionPrompt.transform.position =
                buttonTransform.position +
                Vector3.up * promptHeight;

            // Make the text face the camera.
            if (viewCamera != null)
            {
                interactionPrompt.transform.rotation =
                    viewCamera.transform.rotation;
            }
        }
    }


    // ==========================================
    // HIDE INTERACTION PROMPT
    // ==========================================

    private void HidePrompt()
    {
        // Hide text when no button is detected.
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }
}
