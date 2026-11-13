
using UnityEngine;

public class MirrorButton : MonoBehaviour
{
    // The two possible button actions.
    public enum ButtonAction
    {
        RotateMirror,
        ActivateLaser
    }

    [Header("Button Action")]
    public ButtonAction buttonAction =
        ButtonAction.RotateMirror;

    [Header("Mirror Settings")]
    public Transform mirror;

    [Header("Laser Settings")]
    public LaserPuzzle laserPuzzle;

    // Rotate by exactly 45 degrees per click.
    private const float rotationStep = 45f;

    private float currentAngle;
    private Transform trackedMirror;

    // Prevent duplicate clicks in one frame.
    private int lastInteractionFrame = -1;


    // The interaction message shown to players.
    public string PromptText
    {
        get
        {
            if (buttonAction == ButtonAction.ActivateLaser)
            {
                if (laserPuzzle == null)
                    return "Laser button not configured";

                return laserPuzzle.IsLaserOn
                    ? "Left click to turn laser OFF"
                    : "Left click to turn laser ON";
            }

            return "Left click to rotate mirror";
        }
    }


    // Runs when the game starts.
    private void Start()
    {
        SetupMirror();
    }


    // Read the mirror's starting Y rotation.
    private void SetupMirror()
    {
        if (mirror == null)
            return;

        trackedMirror = mirror;

        currentAngle = Mathf.Repeat(
            mirror.localEulerAngles.y,
            360f
        );
    }


    // Called by PlayerInteract when clicked.
    public void Interact()
    {
        // Prevent duplicate interaction in one frame.
        if (lastInteractionFrame == Time.frameCount)
            return;

        lastInteractionFrame = Time.frameCount;

        // ACTION 1: Toggle laser ON or OFF.
        if (buttonAction == ButtonAction.ActivateLaser)
        {
            if (laserPuzzle == null)
            {
                Debug.LogWarning(
                    "Laser Puzzle not assigned!"
                );
                return;
            }

            laserPuzzle.ToggleLaser();
            return;
        }

        // ACTION 2: Rotate mirror.
        if (mirror == null)
        {
            Debug.LogWarning("Mirror not assigned!");
            return;
        }

        // Update rotation if assigned mirror changed.
        if (trackedMirror != mirror)
            SetupMirror();

        RotateMirror();
    }


    // Rotate mirror by 45 degrees.
    private void RotateMirror()
    {
        // Add exactly 45 degrees.
        currentAngle += rotationStep;

        // Wrap around after 360 degrees.
        // Example: 315 + 45 becomes 0.
        currentAngle = Mathf.Repeat(
            currentAngle,
            360f
        );

        // Rotate the parent mirror pivot.
        mirror.localRotation = Quaternion.Euler(
            0f,
            currentAngle,
            0f
        );

        Debug.Log(
            mirror.name +
            " rotated to Y = " +
            currentAngle +
            " degrees"
        );
    }
}
