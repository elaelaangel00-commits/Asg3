
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserPuzzle : MonoBehaviour
{
    [Header("Laser Settings")]
    public float maxDistance = 100f;
    public int maxBounces = 5;
    public bool startLaserOn = false;
    public int requiredUniqueMirrors = 3;
    public bool debugLaserHits = true;

    [Header("Exit Door")]
    public Transform exitDoor;
    public float doorOpenAngle = 90f;
    public float doorRotationSpeed = 90f;

    [Header("Next Level")]
    public ExitDoorTrigger exitTrigger;

    private LineRenderer laser;
    private bool laserOn;
    private bool unlocked;
    private bool doorRotationCaptured;
    private bool passageUnlocked;

    private Quaternion closedDoorRotation;
    private string lastLaserPath;

    private readonly HashSet<int> uniqueMirrors =
        new HashSet<int>();

    public bool IsLaserOn => laserOn;

    private void Awake()
    {
        laser = GetComponent<LineRenderer>();
        laser.useWorldSpace = true;

        laserOn = startLaserOn;
        laser.enabled = laserOn;
    }

    public void ActivateLaser()
    {
        if (laserOn)
            return;

        laserOn = true;
        laser.enabled = true;

        Debug.Log("Laser activated!");
    }

    private void Update()
    {
        if (exitDoor != null && !doorRotationCaptured)
        {
            closedDoorRotation = exitDoor.localRotation;
            doorRotationCaptured = true;
        }

        if (laserOn)
        {
            Physics.SyncTransforms();
            TraceLaser();
        }

        if (!unlocked)
            return;

        if (exitDoor != null && doorRotationCaptured)
        {
            Quaternion openRotation =
                closedDoorRotation *
                Quaternion.Euler(
                    0f,
                    doorOpenAngle,
                    0f
                );

            exitDoor.localRotation =
                Quaternion.RotateTowards(
                    exitDoor.localRotation,
                    openRotation,
                    doorRotationSpeed * Time.deltaTime
                );

            if (!passageUnlocked &&
                Quaternion.Angle(
                    exitDoor.localRotation,
                    openRotation
                ) < 1f)
            {
                UnlockPassage();
            }
        }
        else if (!passageUnlocked)
        {
            UnlockPassage();
        }
    }

    private void UnlockPassage()
    {
        passageUnlocked = true;

        if (exitTrigger != null)
        {
            exitTrigger.Unlock();
        }
        else
        {
            Debug.LogWarning(
                "Exit Trigger is not assigned!"
            );
        }
    }

    private void TraceLaser()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward.normalized;

        uniqueMirrors.Clear();

        string path = "Emitter";

        laser.positionCount = 1;
        laser.SetPosition(0, origin);

        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            if (!Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                maxDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide))
            {
                AddPoint(
                    origin + direction * maxDistance
                );

                path += " -> No hit";
                break;
            }

            AddPoint(hit.point);

            Transform receiver = FindTaggedAncestor(
                hit.collider.transform,
                "Receiver"
            );

            Transform mirror = FindTaggedAncestor(
                hit.collider.transform,
                "Mirror"
            );

            if (receiver != null)
            {
                path += " -> Receiver";

                if (uniqueMirrors.Count >= requiredUniqueMirrors)
                {
                    if (!unlocked)
                    {
                        unlocked = true;

                        Debug.Log(
                            "All mirrors reached! Door opening!"
                        );
                    }
                }
                else
                {
                    path += " (not enough mirrors)";
                }

                break;
            }

            if (mirror != null && bounce < maxBounces)
            {
                uniqueMirrors.Add(
                    mirror.GetInstanceID()
                );

                path += " -> " + mirror.name;

                direction = Vector3.Reflect(
                    direction,
                    hit.normal
                ).normalized;

                origin =
                    hit.point + direction * 0.03f;

                continue;
            }

            path += " -> Blocked by " +
                    hit.collider.name;

            break;
        }

        if (debugLaserHits && path != lastLaserPath)
        {
            Debug.Log("Laser path: " + path);
            lastLaserPath = path;
        }
    }

    private void AddPoint(Vector3 position)
    {
        int count = laser.positionCount;

        laser.positionCount = count + 1;
        laser.SetPosition(count, position);
    }

    private Transform FindTaggedAncestor(
        Transform target,
        string targetTag)
    {
        while (target != null)
        {
            if (target.CompareTag(targetTag))
                return target;

            target = target.parent;
        }

        return null;
    }
}
