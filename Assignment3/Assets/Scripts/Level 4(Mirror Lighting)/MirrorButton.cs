
using UnityEngine;

public class MirrorButton : MonoBehaviour
{
    // ==========================================
    // MIRROR SETTINGS
    // ==========================================

    [Header("Mirror Settings")]

    // Drag the mirror you want this button
    // to control into this field.
    public Transform mirror;

    // Each mouse click rotates the mirror
    // by exactly 45 degrees.
    //
    // const = a value that cannot be changed
    // while the game is running.
    private const float rotationStep = 45f;


    // ==========================================
    // PRIVATE VARIABLES
    // ==========================================

    // Stores the current Y rotation.
    private float currentAngle;

    // Remembers which mirror is assigned.
    private Transform trackedMirror;

    // Prevents the button from being activated
    // more than once in the same frame.
    private int lastInteractionFrame = -1;


    // ==========================================
    // INTERACTION PROMPT
    // ==========================================

    // PlayerInteract reads this text when
    // the player points at the button.
    public string PromptText
    {
        get
        {
            return "Left click to rotate mirror";
        }
    }


    // ==========================================
    // START
    // ==========================================

    // Start runs once when the game begins.
    private void Start()
    {
        SetupMirror();
    }


    // ==========================================
    // SET UP MIRROR
    // ==========================================

    private void SetupMirror()
    {
        // Stop if no mirror is assigned.
        if (mirror == null)
        {
            return;
        }

        // Remember the assigned mirror.
        trackedMirror = mirror;

        // Read its current Y rotation.
        //
        // localEulerAngles = rotation relative
        // to the parent GameObject.
        //
        // Mathf.Repeat keeps the angle
        // between 0 and 360 degrees.
        currentAngle = Mathf.Repeat(
            mirror.localEulerAngles.y,
            360f
        );
    }


    // ==========================================
    // PLAYER INTERACTION
    // ==========================================

    // PlayerInteract calls this function
    // when the player clicks this button.
    public void Interact()
    {
        // Prevent duplicate interactions
        // during the same frame.
        if (lastInteractionFrame == Time.frameCount)
        {
            return;
        }

        lastInteractionFrame = Time.frameCount;


        // Check whether a mirror was assigned.
        if (mirror == null)
        {
            Debug.LogWarning(
                "Mirror Button: No mirror assigned!"
            );

            return;
        }


        // If the assigned mirror changes,
        // update its starting rotation.
        if (trackedMirror != mirror)
        {
            SetupMirror();
        }


        // Rotate the assigned mirror.
        RotateMirror();
    }


    // ==========================================
    // ROTATE MIRROR
    // ==========================================

    private void RotateMirror()
    {
        // Add 45 degrees to the current angle.
        currentAngle += rotationStep;

        // Keep rotation between 0 and 360.
        //
        // Example:
        // 315 + 45 = 360
        // Mathf.Repeat(360, 360) = 0
        currentAngle = Mathf.Repeat(
            currentAngle,
            360f
        );


        // Apply rotation to the mirror.
        //
        // Quaternion.Euler creates a rotation
        // using X, Y and Z angles.
        //
        // We only rotate around the Y-axis.
        mirror.localRotation = Quaternion.Euler(
            0f,
            currentAngle,
            0f
        );


        // Display information in Unity Console
        // to help with debugging.
        Debug.Log(
            mirror.name +
            " rotated to Y = " +
            currentAngle +
            " degrees"
        );
    }
}
