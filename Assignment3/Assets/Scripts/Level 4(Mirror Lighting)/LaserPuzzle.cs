
using UnityEngine;
using TMPro;

// Required Unity components.
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]

public class LaserPuzzle : MonoBehaviour
{
    // ==========================================
    // LASER SETTINGS
    // ==========================================

    [Header("Laser Settings")]

    // Should the laser start ON or OFF?
    public bool startLaserOn = false;

    // Maximum distance the laser can travel
    // before stopping.
    public float maxDistance = 30f;

    // Maximum number of mirror reflections.
    public int maxReflections = 10;


    // ==========================================
    // GARGOYLE MOUTH
    // ==========================================

    [Header("Gargoyle Mouth")]

    // Where the laser starts.
    // Drag the gargoyle's mouth point here.
    public Transform mouthPoint;

    // Moves the laser slightly forward
    // to avoid hitting its own source.
    public float mouthForwardOffset = 0.12f;


    // ==========================================
    // FIVE MIRRORS
    // ==========================================

    [Header("Five Mirrors")]

    // Arrange the mirrors in the correct order:
    //
    // Element 0 = Yellow
    // Element 1 = Purple
    // Element 2 = Red
    // Element 3 = Blue
    // Element 4 = Green
    //
    // The puzzle checks the order of the
    // references in this array.
    public Transform[] mirrors = new Transform[5];


    // ==========================================
    // RECEIVER
    // ==========================================

    [Header("Receiver")]

    // The object the laser must reach
    // after reflecting from all five mirrors.
    public Transform receiver;


    // ==========================================
    // RECEIVER GARGOYLE
    // ==========================================

    [Header("Receiver Gargoyle")]

    // The gargoyle that changes colour.
    public GameObject receiverGargoyle;

    // Green material applied after success.
    public Material greenMaterial;


    // ==========================================
    // PUZZLE SUCCESS AUDIO
    // ==========================================

    [Header("Puzzle Success Sound")]

    // Sound played when the puzzle is solved.
    public AudioClip successSound;

    [Range(0f, 1f)]
    public float successVolume = 1f;


    // ==========================================
    // LEVEL PROGRESS HUD - NEW
    // ==========================================

    [Header("Level Progress HUD")]

    // Drag your LevelProgressText here.
    //
    // This displays how many puzzles remain
    // before the exit can be unlocked.
    public TMP_Text progressText;


    // ==========================================
    // EXIT DOOR
    // ==========================================

    [Header("Exit Door")]

    // The actual door that rotates open.
    public Transform exitDoor;

    // Trigger that loads the next level.
    public ExitDoorTrigger exitTrigger;

    // How many degrees the door opens.
    public float doorOpenAngle = 90f;

    // Door rotation speed.
    public float doorRotationSpeed = 90f;


    // ==========================================
    // DEBUG SETTINGS
    // ==========================================

    [Header("Debug")]

    // Enable Console messages for testing.
    public bool showDebugLogs = true;


    // ==========================================
    // PRIVATE VARIABLES
    // ==========================================

    // Draws the visible laser beam.
    private LineRenderer laserLine;

    // Plays the success sound.
    private AudioSource audioSource;

    // Is the laser currently active?
    private bool laserOn;

    // Has the player completed the puzzle?
    private bool solved;

    // Has the exit finished unlocking?
    private bool exitUnlocked;

    // Remembers the door's original rotation.
    private Quaternion closedDoorRotation;

    // Prevents repeated Console messages.
    private string lastPath = "";


    // ==========================================
    // PUBLIC LASER STATE
    // ==========================================

    // LaserButton.cs reads this property
    // to display ON/OFF instructions.
    public bool IsLaserOn => laserOn;


    // ==========================================
    // AWAKE
    // ==========================================

    private void Awake()
    {
        // Get the required components.
        laserLine = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();


        // Use world coordinates for the laser.
        laserLine.useWorldSpace = true;


        // Prevent automatic audio playback.
        audioSource.playOnAwake = false;

        // Use 2D audio for puzzle success.
        audioSource.spatialBlend = 0f;


        // Set the initial laser state.
        laserOn = startLaserOn;

        // Show or hide the beam.
        laserLine.enabled = laserOn;

        // Remove any previous laser points.
        laserLine.positionCount = 0;


        // Remember the door's starting rotation.
        if (exitDoor != null)
        {
            closedDoorRotation = exitDoor.localRotation;
        }


        // ======================================
        // INITIALISE LEVEL PROGRESS HUD - NEW
        // ======================================

        // At the beginning, the player still
        // has one laser puzzle to complete.
        if (progressText != null)
        {
            progressText.text =
                "Puzzles Remaining: 1\nEXIT LOCKED";
        }
    }


    // ==========================================
    // TOGGLE LASER ON / OFF
    // ==========================================

    // Called by LaserButton.cs.
    public void ToggleLaser()
    {
        // The ! operator reverses a Boolean.
        //
        // true becomes false.
        // false becomes true.
        laserOn = !laserOn;


        // Update the visible laser.
        laserLine.enabled = laserOn;


        // Clear the laser when switched OFF.
        if (!laserOn)
        {
            laserLine.positionCount = 0;
        }


        Debug.Log(
            laserOn ? "Laser ON" : "Laser OFF"
        );
    }


    // ==========================================
    // UPDATE
    // ==========================================

    // Runs once every frame.
    private void Update()
    {
        // If the laser is ON,
        // calculate its reflection path.
        if (laserOn)
        {
            // Update physics after mirror rotations.
            Physics.SyncTransforms();

            // Trace the laser beam.
            TraceLaser();
        }


        // Once solved, gradually open the door.
        if (solved)
        {
            OpenDoor();
        }
    }


    // ==========================================
    // TRACE LASER
    // ==========================================

    private void TraceLaser()
    {
        // Use the mouth point if assigned.
        // Otherwise use this object's Transform.
        Transform source = mouthPoint != null
            ? mouthPoint
            : transform;


        // The blue Z-axis is forward in Unity.
        Vector3 direction = source.forward.normalized;


        // Start slightly in front of the mouth.
        Vector3 origin = source.position +
            direction * mouthForwardOffset;


        // Start drawing the visible beam.
        laserLine.positionCount = 1;
        laserLine.SetPosition(0, origin);


        // Track which mirror should be hit next.
        //
        // 0 = Yellow
        // 1 = Purple
        // 2 = Red
        // 3 = Blue
        // 4 = Green
        int nextMirror = 0;

        // Assume correct order until proven wrong.
        bool correctOrder = true;

        // Debug text showing the laser path.
        string path = "Gargoyle Mouth";


        // ======================================
        // REFLECTION LOOP
        // ======================================

        // Continue tracing until the laser
        // hits something or reaches its limit.
        for (int bounce = 0;
             bounce <= maxReflections;
             bounce++)
        {
            // Shoot an invisible physics ray.
            bool hitSomething = Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                maxDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );


            // ==================================
            // CASE 1: NOTHING WAS HIT
            // ==================================

            if (!hitSomething)
            {
                // Draw the beam into empty space.
                AddPoint(
                    origin + direction * maxDistance
                );

                LogPath(
                    path + " -> Empty Space"
                );

                return;
            }


            // ==================================
            // CASE 2: SOMETHING WAS HIT
            // ==================================

            // Draw the beam to the collision point.
            AddPoint(hit.point);


            // Check whether the object is a mirror.
            int mirrorIndex = FindMirror(
                hit.collider.transform
            );


            // ==================================
            // CASE 3: MIRROR DETECTED
            // ==================================

            if (mirrorIndex != -1)
            {
                path +=
                    " -> Mirror " + (mirrorIndex + 1);


                // Check whether this is the
                // correct next mirror.
                if (correctOrder &&
                    mirrorIndex == nextMirror)
                {
                    // Correct mirror!
                    // Expect the next one.
                    nextMirror++;
                }
                else
                {
                    // Wrong mirror order.
                    correctOrder = false;
                }


                // Prevent infinite reflections.
                if (bounce >= maxReflections)
                {
                    LogPath(
                        path + " -> Reflection Limit"
                    );

                    return;
                }


                // ==================================
                // REFLECT THE LASER
                // ==================================

                // Vector3.Reflect calculates the
                // direction after reflection.
                //
                // hit.normal is perpendicular
                // to the mirror surface.
                direction = Vector3.Reflect(
                    direction,
                    hit.normal
                ).normalized;


                // Move the origin slightly away
                // from the mirror surface.
                //
                // This prevents the ray from
                // repeatedly hitting the same spot.
                origin = hit.point +
                    direction * 0.04f;


                // Continue to the next reflection.
                continue;
            }


            // ==================================
            // CASE 4: RECEIVER DETECTED
            // ==================================

            if (IsPartOf(
                hit.collider.transform,
                receiver))
            {
                path += " -> Receiver";


                // Puzzle succeeds ONLY when:
                //
                // 1. Mirrors are hit in order.
                // 2. There are exactly 5 mirrors.
                // 3. All 5 have been reached.
                bool correctSolution =
                    correctOrder &&
                    mirrors.Length == 5 &&
                    nextMirror == 5;


                // ==================================
                // PUZZLE SOLVED!
                // ==================================

                if (correctSolution && !solved)
                {
                    // Mark puzzle as completed.
                    solved = true;


                    // ==============================
                    // UPDATE HUD - NEW
                    // ==============================

                    // No puzzles remain.
                    // Door is still opening.
                    if (progressText != null)
                    {
                        progressText.text =
                            "Puzzles Remaining: 0\n" +
                            "EXIT OPENING...";
                    }


                    // Change receiving gargoyle
                    // to green material.
                    TurnReceiverGreen();


                    // Play success sound once.
                    PlaySuccessSound();


                    Debug.Log(
                        "PUZZLE SOLVED! Door opening!"
                    );
                }
                else if (!correctSolution && !solved)
                {
                    path +=
                        " (Incorrect mirror order)";
                }


                LogPath(path);
                return;
            }


            // ==================================
            // CASE 5: WALL OR OBSTACLE
            // ==================================

            // Stop the laser when it hits
            // an obstacle that is not a mirror
            // or the receiver.
            LogPath(
                path + " -> Blocked by " +
                hit.collider.name
            );

            return;
        }
    }


    // ==========================================
    // FIND MIRROR
    // ==========================================

    private int FindMirror(Transform hitObject)
    {
        // Check each mirror in the array.
        for (int i = 0; i < mirrors.Length; i++)
        {
            if (IsPartOf(hitObject, mirrors[i]))
            {
                // Return the mirror index.
                return i;
            }
        }

        // -1 means no mirror was found.
        return -1;
    }


    // ==========================================
    // CHECK OBJECT RELATIONSHIP
    // ==========================================

    private bool IsPartOf(
        Transform hitObject,
        Transform target)
    {
        // Avoid checking missing objects.
        if (hitObject == null || target == null)
        {
            return false;
        }

        // Return true if the hit object
        // is the target or one of its children.
        return hitObject == target ||
               hitObject.IsChildOf(target);
    }


    // ==========================================
    // ADD LASER POINT
    // ==========================================

    private void AddPoint(Vector3 point)
    {
        // Get current number of points.
        int count = laserLine.positionCount;

        // Add one more point.
        laserLine.positionCount = count + 1;

        // Set the position of the new point.
        laserLine.SetPosition(count, point);
    }


    // ==========================================
    // TURN RECEIVER GARGOYLE GREEN
    // ==========================================

    private void TurnReceiverGreen()
    {
        // Make sure both references exist.
        if (receiverGargoyle == null ||
            greenMaterial == null)
        {
            Debug.LogWarning(
                "Assign Receiver Gargoyle and Green Material!"
            );

            return;
        }


        // Find renderers on the gargoyle
        // and all its child objects.
        Renderer[] renderers =
            receiverGargoyle.GetComponentsInChildren
                <Renderer>(true);


        foreach (Renderer rend in renderers)
        {
            // Only modify 3D mesh renderers.
            if (!(rend is MeshRenderer) &&
                !(rend is SkinnedMeshRenderer))
            {
                continue;
            }


            // Get the object's material slots.
            Material[] materials = rend.sharedMaterials;


            // Change every slot to green.
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = greenMaterial;
            }


            // Apply the new materials.
            rend.sharedMaterials = materials;
        }


        Debug.Log(
            "Receiver Gargoyle turned GREEN!"
        );
    }


    // ==========================================
    // PLAY SUCCESS SOUND
    // ==========================================

    private void PlaySuccessSound()
    {
        // Check whether an AudioClip exists.
        if (successSound == null)
        {
            Debug.LogWarning(
                "Puzzle Success Sound is not assigned!"
            );

            return;
        }


        // Play the sound once.
        audioSource.PlayOneShot(
            successSound,
            successVolume
        );


        Debug.Log(
            "Puzzle success sound played!"
        );
    }


    // ==========================================
    // OPEN EXIT DOOR
    // ==========================================

    private void OpenDoor()
    {
        // Check whether the exit door exists.
        if (exitDoor != null)
        {
            // Calculate the target rotation.
            Quaternion targetRotation =
                closedDoorRotation *
                Quaternion.Euler(
                    0f,
                    doorOpenAngle,
                    0f
                );


            // Rotate the door smoothly.
            exitDoor.localRotation =
                Quaternion.RotateTowards(
                    exitDoor.localRotation,
                    targetRotation,
                    doorRotationSpeed * Time.deltaTime
                );


            // Wait until the door finishes opening.
            if (Quaternion.Angle(
                exitDoor.localRotation,
                targetRotation) > 0.5f)
            {
                return;
            }
        }


        // ======================================
        // UNLOCK EXIT
        // ======================================

        // Only unlock once.
        if (!exitUnlocked)
        {
            exitUnlocked = true;


            // Tell the exit trigger
            // that the player can proceed.
            if (exitTrigger != null)
            {
                exitTrigger.Unlock();


                // ==================================
                // FINAL HUD UPDATE - NEW
                // ==================================

                // Show the player that the
                // exit is now unlocked.
                if (progressText != null)
                {
                    progressText.text =
                        "Mirror Puzzle: 0\n" +
                        "EXIT UNLOCKED";
                }
            }
            else
            {
                Debug.LogWarning(
                    "Exit Trigger not assigned!"
                );
            }
        }
    }


    // ==========================================
    // DEBUG LASER PATH
    // ==========================================

    private void LogPath(string path)
    {
        // Only print when the laser path changes.
        //
        // This prevents Console spam.
        if (showDebugLogs && path != lastPath)
        {
            Debug.Log("Laser Path: " + path);
            lastPath = path;
        }
    }


    // ==========================================
    // SCENE VIEW GIZMOS
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        // Use mouth point if assigned.
        Transform source = mouthPoint != null
            ? mouthPoint
            : transform;


        // Direction the laser starts travelling.
        Vector3 direction =
            source.forward.normalized;


        // Calculate starting position.
        Vector3 origin = source.position +
            direction * mouthForwardOffset;


        // Draw yellow sphere at laser origin.
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(origin, 0.1f);


        // Draw a red line showing
        // the initial laser direction.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(
            origin,
            direction * 3f
        );
    }
}
