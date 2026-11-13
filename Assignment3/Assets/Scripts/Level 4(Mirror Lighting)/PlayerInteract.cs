
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Automatically requires an AudioSource.
[RequireComponent(typeof(AudioSource))]

public class PlayerInteract : MonoBehaviour
{
    // ==========================================
    // RAYCAST SETTINGS
    // ==========================================

    [Header("Raycast Settings")]

    // Maximum distance for interacting
    // with a button.
    public float interactDistance = 3f;

    // Camera that detects the button.
    public Camera viewCamera;

    // TRUE = aim using mouse cursor.
    // FALSE = aim using screen centre.
    public bool useMouseHover = true;


    // ==========================================
    // INTERACTION SETTINGS
    // ==========================================

    [Header("Interaction")]

    // Allows E to activate buttons if enabled.
    public bool allowEKey = false;


    // ==========================================
    // INTERACTION UI
    // ==========================================

    [Header("Interaction UI")]

    // TextMeshPro prompt displayed to players.
    public TMP_Text interactionPrompt;

    // Enable only for World Space Canvas.
    public bool movePromptAboveButton = false;

    // Vertical offset above the button.
    public float promptHeight = 0.7f;


    // ==========================================
    // BUTTON SOUND
    // ==========================================

    [Header("Button Click Sound")]

    // Sound played when the player clicks.
    public AudioClip selectionSound;

    // Button sound volume.
    [Range(0f, 1f)]
    public float selectionVolume = 1f;

    // AudioSource used to play sound.
    private AudioSource audioSource;


    // ==========================================
    // AWAKE
    // ==========================================

    private void Awake()
    {
        // Get AudioSource from this GameObject.
        audioSource = GetComponent<AudioSource>();

        // Disable automatic audio playback.
        audioSource.playOnAwake = false;

        // Make button clicks 2D audio.
        audioSource.spatialBlend = 0f;
    }


    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        // Try finding a camera attached
        // to this GameObject or its children.
        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        // Otherwise use the Main Camera.
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        // Hide interaction text at the beginning.
        HidePrompt();
    }


    // ==========================================
    // UPDATE
    // ==========================================

    // Update runs once every frame.
    private void Update()
    {
        // Search for either a mirror button
        // or a laser button using raycasting.
        MirrorButton mirrorButton;
        LaserButton laserButton;

        bool foundButton = FindButton(
            out mirrorButton,
            out laserButton
        );


        // Nothing detected?
        // Hide the interaction text.
        if (!foundButton)
        {
            HidePrompt();
            return;
        }


        // ======================================
        // DISPLAY CORRECT INTERACTION PROMPT
        // ======================================

        if (mirrorButton != null)
        {
            // The player is pointing at
            // a mirror rotation button.
            ShowPrompt(
                mirrorButton.PromptText,
                mirrorButton.transform
            );
        }
        else if (laserButton != null)
        {
            // The player is pointing at
            // the laser activation button.
            ShowPrompt(
                laserButton.PromptText,
                laserButton.transform
            );
        }


        // ======================================
        // CHECK PLAYER INPUT
        // ======================================

        // Check for a left mouse click.
        bool mouseClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;


        // Optionally check for E key.
        bool ePressed =
            allowEKey &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;


        // Do not activate anything until clicked.
        //
        // IMPORTANT:
        // Simply hovering over a button
        // will NOT play a sound.
        if (!mouseClicked && !ePressed)
        {
            return;
        }


        // ======================================
        // ACTIVATE THE CORRECT BUTTON
        // ======================================

        if (mirrorButton != null)
        {
            // Mirror button requires a mirror.
            if (mirrorButton.mirror == null)
            {
                return;
            }

            // Rotate the mirror by 45 degrees.
            mirrorButton.Interact();
        }
        else if (laserButton != null)
        {
            // Laser button requires LaserPuzzle.
            if (laserButton.laserPuzzle == null)
            {
                return;
            }

            // Turn the laser ON or OFF.
            laserButton.Interact();
        }
        else
        {
            return;
        }


        // ======================================
        // PLAY BUTTON SOUND
        // ======================================

        // Sound plays only on a mouse click.
        // E-key interaction does not play it.
        if (mouseClicked)
        {
            PlayClickSound();
        }


        // ======================================
        // UPDATE INTERACTION TEXT
        // ======================================

        // Refresh text after interaction.
        //
        // Example:
        // "Turn laser ON" becomes
        // "Turn laser OFF".
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
    // DETECT BUTTONS USING RAYCAST
    // ==========================================

    private bool FindButton(
        out MirrorButton mirrorButton,
        out LaserButton laserButton)
    {
        // out = returns information through
        // these variables to the calling function.
        //
        // Start with no button detected.
        mirrorButton = null;
        laserButton = null;


        // Without a camera we cannot raycast.
        if (viewCamera == null)
        {
            return false;
        }


        // Ray = invisible line used for detection.
        Ray ray;


        // Use mouse position if enabled.
        if (useMouseHover && Mouse.current != null)
        {
            ray = viewCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );
        }
        else
        {
            // Otherwise shoot from screen centre.
            ray = viewCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );
        }


        // Shoot a ray and check if it hits
        // a 3D Collider within interaction distance.
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide))
        {
            // ----------------------------------
            // CHECK FOR MIRROR BUTTON
            // ----------------------------------

            // Search the detected GameObject
            // and its parents for MirrorButton.
            mirrorButton = hit.collider
                .GetComponentInParent<MirrorButton>();

            if (mirrorButton != null)
            {
                return true;
            }


            // ----------------------------------
            // CHECK FOR LASER BUTTON
            // ----------------------------------

            // If no MirrorButton was found,
            // check for a LaserButton instead.
            laserButton = hit.collider
                .GetComponentInParent<LaserButton>();

            if (laserButton != null)
            {
                return true;
            }
        }


        // Neither type of button was found.
        return false;
    }


    // ==========================================
    // PLAY BUTTON SOUND
    // ==========================================

    private void PlayClickSound()
    {
        // Do nothing if no AudioClip is assigned.
        if (selectionSound == null)
        {
            return;
        }

        // PlayOneShot plays the clip once.
        // It doesn't automatically loop.
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
        // Stop if no TMP text is assigned.
        if (interactionPrompt == null)
        {
            return;
        }

        // Display the interaction text.
        interactionPrompt.gameObject.SetActive(true);

        // Show the instructions from the
        // detected button.
        interactionPrompt.text = promptText;


        // OPTIONAL:
        // Move text above the actual button.
        // Use only for World Space Canvas.
        if (movePromptAboveButton &&
            buttonTransform != null)
        {
            interactionPrompt.transform.position =
                buttonTransform.position +
                Vector3.up * promptHeight;

            // Rotate prompt to face the camera.
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
        // Hide the prompt when the player
        // isn't pointing at a button.
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }
}
