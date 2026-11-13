
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
public class LaserPuzzle : MonoBehaviour
{
    [Header("Laser Settings")]
    public bool startLaserOn = false;
    public float maxDistance = 30f;
    public int maxReflections = 10;

    [Header("Gargoyle Mouth")]
    public Transform mouthPoint;
    public float mouthForwardOffset = 0.12f;

    [Header("Five Mirrors")]
    public Transform[] mirrors = new Transform[5];

    [Header("Receiver")]
    public Transform receiver;

    [Header("Receiver Gargoyle")]
    public GameObject receiverGargoyle;
    public Material greenMaterial;

    [Header("Puzzle Success Sound")]
    public AudioClip successSound;

    [Range(0f, 1f)]
    public float successVolume = 1f;

    [Header("Exit Door")]
    public Transform exitDoor;
    public ExitDoorTrigger exitTrigger;
    public float doorOpenAngle = 90f;
    public float doorRotationSpeed = 90f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Components used by this puzzle.
    private LineRenderer laserLine;
    private AudioSource audioSource;

    // Current puzzle states.
    private bool laserOn;
    private bool solved;
    private bool exitUnlocked;

    private Quaternion closedDoorRotation;
    private string lastPath = "";

    // Allows MirrorButton to check ON/OFF state.
    public bool IsLaserOn => laserOn;


    // Runs when the GameObject is initialized.
    private void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Laser positions use world coordinates.
        laserLine.useWorldSpace = true;

        // Keep success sound clear and audible.
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        laserOn = startLaserOn;
        laserLine.enabled = laserOn;
        laserLine.positionCount = 0;

        // Remember the door's starting angle.
        if (exitDoor != null)
            closedDoorRotation = exitDoor.localRotation;
    }


    // Called by the laser activation button.
    public void ToggleLaser()
    {
        // The ! operator reverses a Boolean.
        // true becomes false and vice versa.
        laserOn = !laserOn;

        laserLine.enabled = laserOn;

        // Clear old beam when laser is OFF.
        if (!laserOn)
            laserLine.positionCount = 0;

        Debug.Log(laserOn ? "Laser ON" : "Laser OFF");
    }


    // Runs once every frame.
    private void Update()
    {
        if (laserOn)
        {
            // Update mirror Collider positions.
            Physics.SyncTransforms();

            // Recalculate the reflections.
            TraceLaser();
        }

        if (solved)
            OpenDoor();
    }


    // Main function: shoot and reflect the laser.
    private void TraceLaser()
    {
        // Use the mouth position if assigned.
        Transform source = mouthPoint != null
            ? mouthPoint : transform;

        // Blue Z-axis = forward direction.
        Vector3 direction = source.forward.normalized;

        // Start slightly outside Gargoyle's mouth.
        Vector3 origin = source.position +
            direction * mouthForwardOffset;

        // Clear the previous laser path.
        laserLine.positionCount = 1;
        laserLine.SetPosition(0, origin);

        // Track the sequence: Mirror 1 to Mirror 5.
        int nextMirror = 0;
        bool correctOrder = true;

        string path = "Gargoyle Mouth";

        // Each loop traces another laser segment.
        for (int bounce = 0;
             bounce <= maxReflections;
             bounce++)
        {
            // RAYCAST:
            // Shoot an invisible ray from origin
            // in the current direction.
            bool hitSomething = Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                maxDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            // CASE 1: The ray hits nothing.
            if (!hitSomething)
            {
                // Draw the laser into empty space.
                AddPoint(
                    origin + direction * maxDistance
                );

                LogPath(path + " -> Empty Space");
                return;
            }

            // CASE 2: The ray hits a Collider.
            // Draw the laser up to the hit point.
            AddPoint(hit.point);

            // Check whether the object is a mirror.
            int mirrorIndex = FindMirror(
                hit.collider.transform
            );

            // CASE 3: The ray hits a mirror.
            if (mirrorIndex != -1)
            {
                path += " -> Mirror " + (mirrorIndex + 1);

                // Remember if the sequence is correct.
                if (correctOrder &&
                    mirrorIndex == nextMirror)
                {
                    nextMirror++;
                }
                else
                {
                    correctOrder = false;
                }

                // Prevent infinite reflections.
                if (bounce >= maxReflections)
                {
                    LogPath(path + " -> Reflection Limit");
                    return;
                }

                // REFLECTION:
                // hit.normal is perpendicular to
                // the mirror's Collider surface.
                //
                // Calculate the outgoing direction.
                direction = Vector3.Reflect(
                    direction,
                    hit.normal
                ).normalized;

                // Move the next ray slightly outside
                // the mirror to avoid a repeated hit.
                origin = hit.point +
                    direction * 0.04f;

                // Repeat raycasting in new direction.
                continue;
            }

            // CASE 4: The ray reaches the receiver.
            if (IsPartOf(
                hit.collider.transform,
                receiver))
            {
                path += " -> Receiver";

                // Only succeed after all five mirrors
                // have been hit in the correct order.
                bool correctSolution =
                    correctOrder &&
                    mirrors.Length == 5 &&
                    nextMirror == 5;

                if (correctSolution && !solved)
                {
                    solved = true;

                    // Change Gargoyle 2 to green.
                    TurnReceiverGreen();

                    // Play the success sound ONCE.
                    PlaySuccessSound();

                    Debug.Log(
                        "PUZZLE SOLVED! Door opening!"
                    );
                }
                else if (!correctSolution && !solved)
                {
                    path += " (Incorrect mirror order)";
                }

                LogPath(path);
                return;
            }

            // CASE 5: A wall or obstacle is hit.
            // Stop the visible laser here.
            LogPath(
                path + " -> Blocked by " +
                hit.collider.name
            );

            return;
        }
    }


    // Find which of the five mirrors was hit.
    private int FindMirror(Transform hitObject)
    {
        for (int i = 0; i < mirrors.Length; i++)
        {
            if (IsPartOf(hitObject, mirrors[i]))
                return i;
        }

        // -1 means the object isn't a mirror.
        return -1;
    }


    // Check whether a hit belongs to an object
    // or one of its child GameObjects.
    private bool IsPartOf(
        Transform hitObject,
        Transform target)
    {
        if (hitObject == null || target == null)
            return false;

        return hitObject == target ||
               hitObject.IsChildOf(target);
    }


    // Add a point to the visible LineRenderer.
    private void AddPoint(Vector3 point)
    {
        int count = laserLine.positionCount;

        laserLine.positionCount = count + 1;
        laserLine.SetPosition(count, point);
    }


    // Change Gargoyle 2 to the green material.
    private void TurnReceiverGreen()
    {
        if (receiverGargoyle == null ||
            greenMaterial == null)
        {
            Debug.LogWarning(
                "Assign Receiver Gargoyle and Green Material!"
            );
            return;
        }

        // Find all 3D meshes within Gargoyle 2.
        Renderer[] renderers =
            receiverGargoyle.GetComponentsInChildren
                <Renderer>(true);

        foreach (Renderer rend in renderers)
        {
            // Only modify 3D mesh renderers.
            if (!(rend is MeshRenderer) &&
                !(rend is SkinnedMeshRenderer))
                continue;

            // Read the material slots.
            Material[] materials = rend.sharedMaterials;

            // Replace each slot with green.
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = greenMaterial;
            }

            // Apply materials to this renderer.
            rend.sharedMaterials = materials;
        }

        Debug.Log("Receiver Gargoyle turned GREEN!");
    }


    // NEW: Play sound when the puzzle is solved.
    private void PlaySuccessSound()
    {
        if (successSound == null)
        {
            Debug.LogWarning(
                "Puzzle Success Sound is not assigned!"
            );
            return;
        }

        // PlayOneShot plays an audio clip once.
        // It does not loop automatically.
        audioSource.PlayOneShot(
            successSound,
            successVolume
        );

        Debug.Log("Puzzle success sound played!");
    }


    // Open door after successful puzzle.
    private void OpenDoor()
    {
        if (exitDoor != null)
        {
            // Calculate door's target rotation.
            Quaternion targetRotation =
                closedDoorRotation *
                Quaternion.Euler(0f, doorOpenAngle, 0f);

            // Gradually rotate toward target.
            exitDoor.localRotation =
                Quaternion.RotateTowards(
                    exitDoor.localRotation,
                    targetRotation,
                    doorRotationSpeed * Time.deltaTime
                );

            // Wait until opening is complete.
            if (Quaternion.Angle(
                exitDoor.localRotation,
                targetRotation) > 0.5f)
                return;
        }

        // Unlock passage only once.
        if (!exitUnlocked)
        {
            exitUnlocked = true;

            if (exitTrigger != null)
                exitTrigger.Unlock();
            else
                Debug.LogWarning("Exit Trigger not assigned!");
        }
    }


    // Prevent identical Console messages
    // from being printed every frame.
    private void LogPath(string path)
    {
        if (showDebugLogs && path != lastPath)
        {
            Debug.Log("Laser Path: " + path);
            lastPath = path;
        }
    }


    // Debug visuals in Unity Scene view.
    private void OnDrawGizmosSelected()
    {
        Transform source = mouthPoint != null
            ? mouthPoint : transform;

        Vector3 direction = source.forward.normalized;

        Vector3 origin = source.position +
            direction * mouthForwardOffset;

        // Yellow sphere = laser starting point.
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(origin, 0.1f);

        // Red line = laser's initial direction.
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, direction * 3f);
    }
}
