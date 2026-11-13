
using UnityEngine;

public class LaserButton : MonoBehaviour
{
    // ==========================================
    // LASER SETTINGS
    // ==========================================

    [Header("Laser Settings")]

    // Drag the GameObject containing
    // LaserPuzzle.cs into this field.
    //
    // This lets the button communicate
    // with the main laser puzzle.
    public LaserPuzzle laserPuzzle;


    // ==========================================
    // PRIVATE VARIABLES
    // ==========================================

    // Prevent duplicate interactions
    // in the same frame.
    private int lastInteractionFrame = -1;


    // ==========================================
    // INTERACTION PROMPT
    // ==========================================

    // PlayerInteract reads this property
    // to display the correct instruction.
    public string PromptText
    {
        get
        {
            // Check whether LaserPuzzle exists.
            if (laserPuzzle == null)
            {
                return "Laser button not configured";
            }

            // If laser is currently ON,
            // display an instruction to turn it OFF.
            if (laserPuzzle.IsLaserOn)
            {
                return "Left click to turn laser OFF";
            }

            // Otherwise, show the ON instruction.
            return "Left click to turn laser ON";
        }
    }


    // ==========================================
    // PLAYER INTERACTION
    // ==========================================

    // This function is called by PlayerInteract
    // when the player clicks the laser button.
    public void Interact()
    {
        // Stop duplicate interactions
        // during the same frame.
        if (lastInteractionFrame == Time.frameCount)
        {
            return;
        }

        lastInteractionFrame = Time.frameCount;


        // Make sure LaserPuzzle is assigned.
        if (laserPuzzle == null)
        {
            Debug.LogWarning(
                "Laser Button: LaserPuzzle not assigned!"
            );

            return;
        }


        // ToggleLaser() exists in LaserPuzzle.cs.
        //
        // If laser is OFF, it turns ON.
        // If laser is ON, it turns OFF.
        laserPuzzle.ToggleLaser();
    }
}
