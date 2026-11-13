
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// This script needs an AudioSource component.
// Unity automatically adds one if it is missing.
[RequireComponent(typeof(AudioSource))]

public class PlayerInteract : MonoBehaviour
{
    // ==========================================
    // RAYCAST SETTINGS
    // ==========================================

    [Header("Raycast Settings")]

    // Maximum distance the player can interact.
    // Example: 3 means the button must be
    // within 3 Unity units of the camera.
    public float interactDistance = 3f;

    // Camera used to detect the buttons.
    public Camera viewCamera;

    // TRUE: Point at buttons with the mouse.
    // FALSE: Point using the centre of the screen.
    public bool useMouseHover = true;


    // ==========================================
    // INTERACTION SETTINGS
    // ==========================================

    [Header("Interaction")]

    // Enable this if you also want the E key
    // to activate buttons.
    public bool allowEKey = false;


    // ==========================================
    // INTERACTION UI
    // ==========================================

    [Header("Interaction UI")]

    // TextMeshPro text that tells the player
    // what button they are pointing at.
    public TMP_Text interactionPrompt;

    // Enable only if using a World Space Canvas.
    public bool movePromptAboveButton = false;

    // How high the text appears above the button.
    public float promptHeight = 0.7f;


    // ==========================================
    // BUTTON SOUND EFFECT
    // ==========================================

    [Header("Button Click Sound")]

    // Drag your button click sound here.
    public AudioClip selectionSound;

    // Sound volume:
    // 0 = silent
    // 1 = maximum volume
    [Range(0f, 1f)]
    public float selectionVolume = 1f;

    // Stores the AudioSource component.
    // Private means it is managed by this script.
    private AudioSource audioSource;


    // ==========================================
    // AWAKE
    // ==========================================

    private void Awake()
    {
        // Find the AudioSource on this GameObject.
        audioSource = GetComponent<AudioSource>();

        // Prevent sound from playing automatically
        // when the game starts.
        audioSource.playOnAwake = false;

        // 0 = 2D audio.
        // The sound is not affected by distance.
        audioSource.spatialBlend = 0f;
    }


    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        // If no camera is assigned in the Inspector,
        // try finding one attached to the player.
        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        // If still missing, find the Main Camera.
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        // Hide the interaction text at game start.
        HidePrompt();
    }


    // ==========================================
    // UPDATE
    // ==========================================

    // Update runs once every frame.
    private void Update()
    {
        // Check which button the player is pointing at.
        MirrorButton button = FindButton();

        // If the player is not pointing at a button,
        // hide the interaction text.
        if (button == null)
        {
            HidePrompt();
            return;
        }

        // Show the interaction instructions.
        // IMPORTANT:
        // Hovering does NOT play any sound.
        ShowPrompt(button);


        // Check whether the left mouse button
        // was pressed this frame.
        bool mouseClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;


        // Check whether E was pressed.
        // This only works if allowEKey is enabled.
        bool ePressed =
            allowEKey &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;


        // If neither input was pressed,
        // stop here and wait for interaction.
        if (!mouseClicked && !ePressed)
        {
            return;
        }


        // Check whether the button is configured.
        if (!CanInteract(button))
        {
            return;
        }


        // Activate the button.
        // The MirrorButton script decides whether
        // to rotate a mirror or toggle the laser.
        button.Interact();


        // Play a sound ONLY when the player
        // uses the left mouse button.
        if (mouseClicked)
        {
            PlayClickSound();
        }


        // Update the prompt after interacting.
        // Example:
        // "Turn laser ON" becomes "Turn laser OFF".
        ShowPrompt(button);
    }


    // ==========================================
    // FIND BUTTON USING RAYCAST
    // ==========================================

    private MirrorButton FindButton()
    {
        // Without a camera, raycasting cannot work.
        if (viewCamera == null)
        {
            return null;
        }

        // Ray = invisible line used for detection.
        Ray ray;


        // OPTION 1: Use mouse position.
        if (useMouseHover && Mouse.current != null)
        {
            // Shoot the invisible ray from the camera
            // through the mouse cursor position.
            ray = viewCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );
        }
        else
        {
            // OPTION 2: Use screen centre.
            // 0.5, 0.5 means the middle of the screen.
            ray = viewCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );
        }


        // Physics.Raycast checks whether the ray
        // touches a 3D Collider.
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide))
        {
            // Check whether the object has MirrorButton.
            // GetComponentInParent also searches
            // its parent GameObjects.
            return hit.collider
                .GetComponentInParent<MirrorButton>();
        }


        // Nothing detected.
        return null;
    }


    // ==========================================
    // CHECK BUTTON CONFIGURATION
    // ==========================================

    private bool CanInteract(MirrorButton button)
    {
        // If this is the laser activation button,
        // it needs a LaserPuzzle script assigned.
        if (button.buttonAction ==
            MirrorButton.ButtonAction.ActivateLaser)
        {
            return button.laserPuzzle != null;
        }

        // If this is a mirror rotation button,
        // it needs a mirror Transform assigned.
        return button.mirror != null;
    }


    // ==========================================
    // PLAY BUTTON CLICK SOUND
    // ==========================================

    private void PlayClickSound()
    {
        // If no sound is assigned, do nothing.
        if (selectionSound == null)
        {
            return;
        }

        // PlayOneShot plays the sound once.
        // It does not continuously loop.
        audioSource.PlayOneShot(
            selectionSound,
            selectionVolume
        );
    }


    // ==========================================
    // SHOW INTERACTION TEXT
    // ==========================================

    private void ShowPrompt(MirrorButton button)
    {
        // Stop if no TextMeshPro UI is assigned.
        if (interactionPrompt == null)
        {
            return;
        }

        // Make the text visible.
        interactionPrompt.gameObject.SetActive(true);

        // Get the correct instructions
        // from the MirrorButton script.
        interactionPrompt.text = button.PromptText;


        // OPTIONAL:
        // Move the text above the actual button.
        // Only use this with a World Space Canvas.
        if (movePromptAboveButton)
        {
            interactionPrompt.transform.position =
                button.transform.position +
                Vector3.up * promptHeight;

            // Face the text towards the camera.
            if (viewCamera != null)
            {
                interactionPrompt.transform.rotation =
                    viewCamera.transform.rotation;
            }
        }
    }


    // ==========================================
    // HIDE INTERACTION TEXT
    // ==========================================

    private void HidePrompt()
    {
        // Hide the prompt when the player
        // stops pointing at a button.
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }
}
