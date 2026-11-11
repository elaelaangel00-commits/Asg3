
using UnityEngine;

public class RandomMirrorSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject mirrorPrefab;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject laserEmitterPrefab;
    [SerializeField] private GameObject receiverPrefab;

    [Header("Scene Objects")]
    [SerializeField] private Transform exitDoor;
    [SerializeField] private ExitDoorTrigger exitTrigger;

    [Header("Spawn Area - World Coordinates")]
    [SerializeField]
    private Vector2 minXZ =
        new Vector2(-6f, -5f);

    [SerializeField]
    private Vector2 maxXZ =
        new Vector2(6f, 5f);

    [Header("Heights")]
    [SerializeField] private float beamHeight = 1.32f;
    [SerializeField] private float buttonHeight = 0.6f;

    [Header("Puzzle Settings")]
    [SerializeField] private float buttonOffset = 1.5f;
    [SerializeField] private int maxAttempts = 60;
    [SerializeField] private bool startMirrorsUnsolved = true;

    [Header("Mirror Face Direction")]
    [SerializeField]
    private Vector3 localMirrorNormal =
        Vector3.forward;

    private GameObject spawnedPuzzle;

    private void Start()
    {
        SpawnPuzzle();
    }

    public void SpawnPuzzle()
    {
        if (spawnedPuzzle != null)
            return;

        if (mirrorPrefab == null ||
            buttonPrefab == null ||
            laserEmitterPrefab == null ||
            receiverPrefab == null)
        {
            Debug.LogError(
                "Assign all four puzzle prefabs!"
            );
            return;
        }

        if (exitDoor == null)
            Debug.LogWarning("Exit Door is not assigned!");

        if (exitTrigger == null)
            Debug.LogWarning("Exit Trigger is not assigned!");

        if (maxXZ.x - minXZ.x < 7f ||
            maxXZ.y - minXZ.y < 7f)
        {
            Debug.LogError(
                "Spawn area must be at least 7 units wide and deep."
            );
            return;
        }

        string lastFailure = "unknown";

        for (int attempt = 0;
             attempt < Mathf.Max(1, maxAttempts);
             attempt++)
        {
            float[] x = RandomAxis(
                minXZ.x,
                maxXZ.x
            );

            float[] z = RandomAxis(
                minXZ.y,
                maxXZ.y
            );

            // Laser -> Mirror 1 -> Mirror 2
            // -> Mirror 3 -> Receiver

            Vector3[] p = new Vector3[5];

            p[0] = new Vector3(
                x[0], beamHeight, z[0]
            );

            p[1] = new Vector3(
                x[1], beamHeight, z[0]
            );

            p[2] = new Vector3(
                x[1], beamHeight, z[1]
            );

            p[3] = new Vector3(
                x[2], beamHeight, z[1]
            );

            p[4] = new Vector3(
                x[2], beamHeight, z[2]
            );

            GameObject root = new GameObject(
                "RandomPuzzle"
            );

            root.transform.SetParent(transform);

            GameObject emitterObject = Instantiate(
                laserEmitterPrefab,
                p[0],
                Quaternion.identity,
                root.transform
            );

            emitterObject.name = "LaserEmitter";

            LaserPuzzle laser = emitterObject
                .GetComponentInChildren<LaserPuzzle>(true);

            if (laser == null)
            {
                Debug.LogError(
                    "LaserEmitter needs LaserPuzzle!"
                );

                Reject(root);
                return;
            }

            laser.exitDoor = exitDoor;
            laser.exitTrigger = exitTrigger;

            laser.maxDistance = Mathf.Max(
                laser.maxDistance,
                100f
            );

            laser.maxBounces = Mathf.Max(
                laser.maxBounces,
                3
            );

            laser.requiredUniqueMirrors = 3;

            // Aim directly toward Mirror 1.
            laser.transform.rotation =
                Quaternion.LookRotation(
                    p[1] - p[0],
                    Vector3.up
                );

            GameObject receiver = Instantiate(
                receiverPrefab,
                p[4],
                receiverPrefab.transform.rotation,
                root.transform
            );

            receiver.name = "Receiver";

            GameObject[] mirrors = new GameObject[3];
            Quaternion[] solutions = new Quaternion[3];

            Vector3 localNormal = new Vector3(
                localMirrorNormal.x,
                0f,
                localMirrorNormal.z
            ).normalized;

            if (localNormal.sqrMagnitude < 0.5f)
            {
                Debug.LogError(
                    "Local Mirror Normal must point along X or Z."
                );

                Reject(root);
                return;
            }

            bool badPrefab = false;

            for (int i = 0; i < 3; i++)
            {
                Vector3 incoming =
                    (p[i + 1] - p[i]).normalized;

                Vector3 outgoing =
                    (p[i + 2] - p[i + 1]).normalized;

                // Required reflection surface normal.
                Vector3 normal =
                    (incoming - outgoing).normalized;

                solutions[i] =
                    Quaternion.FromToRotation(
                        localNormal,
                        normal
                    );

                mirrors[i] = Instantiate(
                    mirrorPrefab,
                    p[i + 1],
                    solutions[i],
                    root.transform
                );

                mirrors[i].name =
                    "Mirror_" + (i + 1);

                if (mirrors[i]
                    .GetComponentInChildren<Collider>(true) == null)
                {
                    badPrefab = true;
                }
            }

            if (badPrefab)
            {
                Debug.LogError(
                    "Mirror prefab needs a 3D Collider!"
                );

                Reject(root);
                return;
            }

            // Validate the solved mirrors first.
            Physics.SyncTransforms();

            if (!ValidateSolvedPath(
                laser,
                mirrors,
                receiver,
                out lastFailure))
            {
                Reject(root);
                continue;
            }

            bool missingButton = false;

            for (int i = 0; i < 3; i++)
            {
                Vector3 incoming =
                    (p[i + 1] - p[i]).normalized;

                Vector3 outgoing =
                    (p[i + 2] - p[i + 1]).normalized;

                Vector3 normal =
                    (incoming - outgoing).normalized;

                Vector3 buttonPosition =
                    p[i + 1] + normal * buttonOffset;

                if (!InsideArea(buttonPosition))
                {
                    buttonPosition =
                        p[i + 1] - normal * buttonOffset;
                }

                buttonPosition.y = buttonHeight;

                GameObject buttonObject = Instantiate(
                    buttonPrefab,
                    buttonPosition,
                    buttonPrefab.transform.rotation,
                    root.transform
                );

                buttonObject.name =
                    "Button_" + (i + 1);

                MirrorButton button = buttonObject
                    .GetComponentInChildren<MirrorButton>(true);

                if (button == null)
                {
                    missingButton = true;
                    break;
                }

                button.buttonAction =
                    MirrorButton.ButtonAction.RotateMirror;

                button.mirror = mirrors[i].transform;
                button.laserPuzzle = null;
                button.rotateAngle = 45f;
            }

            if (missingButton)
            {
                Debug.LogError(
                    "Button prefab needs MirrorButton!"
                );

                Reject(root);
                return;
            }

            // Spawn activation button near the emitter.
            Vector3 activationPosition =
                p[0] +
                Vector3.forward *
                Mathf.Sign(z[1] - z[0]) *
                buttonOffset;

            activationPosition.y = buttonHeight;

            GameObject activationObject = Instantiate(
                buttonPrefab,
                activationPosition,
                buttonPrefab.transform.rotation,
                root.transform
            );

            activationObject.name =
                "LaserActivationButton";

            MirrorButton activation = activationObject
                .GetComponentInChildren<MirrorButton>(true);

            if (activation == null)
            {
                Debug.LogError(
                    "Activation button needs MirrorButton!"
                );

                Reject(root);
                return;
            }

            activation.buttonAction =
                MirrorButton.ButtonAction.ActivateLaser;

            activation.mirror = null;
            activation.laserPuzzle = laser;

            // Buttons must not block the solved path.
            Physics.SyncTransforms();

            if (!ValidateSolvedPath(
                laser,
                mirrors,
                receiver,
                out lastFailure))
            {
                Reject(root);
                continue;
            }

            // Scramble the mirrors only after
            // a valid solution has been confirmed.
            if (startMirrorsUnsolved)
            {
                for (int i = 0; i < 3; i++)
                {
                    int steps = Random.Range(1, 4);

                    mirrors[i].transform.rotation =
                        Quaternion.AngleAxis(
                            -45f * steps,
                            Vector3.up
                        ) * solutions[i];
                }
            }

            Physics.SyncTransforms();

            spawnedPuzzle = root;

            Debug.Log(
                "Solvable laser puzzle generated! " +
                "Start Mirrors Unsolved = " +
                startMirrorsUnsolved
            );

            return;
        }

        Debug.LogError(
            "Could not generate a solvable puzzle. " +
            "Last obstruction: " + lastFailure
        );
    }

    private static float[] RandomAxis(
        float min,
        float max)
    {
        float length = max - min;

        float[] axis = new float[3];

        axis[0] =
            min + length * Random.Range(0.12f, 0.20f);

        axis[1] =
            min + length * Random.Range(0.45f, 0.55f);

        axis[2] =
            min + length * Random.Range(0.80f, 0.88f);

        if (Random.value < 0.5f)
        {
            float temp = axis[0];
            axis[0] = axis[2];
            axis[2] = temp;
        }

        return axis;
    }

    private bool InsideArea(Vector3 p)
    {
        return p.x >= minXZ.x &&
               p.x <= maxXZ.x &&
               p.z >= minXZ.y &&
               p.z <= maxXZ.y;
    }

    private static void Reject(GameObject root)
    {
        root.SetActive(false);
        Object.Destroy(root);
    }

    private bool ValidateSolvedPath(
        LaserPuzzle laser,
        GameObject[] mirrors,
        GameObject receiver,
        out string reason)
    {
        Vector3 origin =
            laser.transform.position;

        Vector3 direction =
            laser.transform.forward.normalized;

        for (int i = 0; i < 3; i++)
        {
            if (!Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                laser.maxDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide))
            {
                reason =
                    "Ray missed Mirror_" + (i + 1);

                return false;
            }

            if (!IsPartOf(hit.collider, mirrors[i]))
            {
                reason =
                    "Expected Mirror_" + (i + 1) +
                    " but hit " + hit.collider.name;

                return false;
            }

            direction = Vector3.Reflect(
                direction,
                hit.normal
            ).normalized;

            origin =
                hit.point + direction * 0.03f;
        }

        if (!Physics.Raycast(
            origin,
            direction,
            out RaycastHit finalHit,
            laser.maxDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide))
        {
            reason =
                "Final beam missed receiver";

            return false;
        }

        if (!IsPartOf(finalHit.collider, receiver))
        {
            reason =
                "Beam hit " + finalHit.collider.name +
                " instead of Receiver";

            return false;
        }

        reason = "success";
        return true;
    }

    private static bool IsPartOf(
        Collider hit,
        GameObject target)
    {
        return hit.transform == target.transform ||
               hit.transform.IsChildOf(target.transform);
    }
}
