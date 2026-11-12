using UnityEngine;

// Allows the player to pick up, carry, and place weighted objects
// Uses a raycast at the center of the screen and checks whether the placment area is clear
public class ObjectPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera; // Used for aiming and interaction raycasts
    [SerializeField] private Transform carryPoint;
    [SerializeField] private Transform playerRoot; // Used to exclude the player from placement checks

    [Header("Pickup")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Crosshair Placement")]
    [SerializeField, Min(0.1f)] private float placementRange = 3f;
    [SerializeField, Min(0f)] private float surfaceGap = 0.02f;
    [SerializeField, Range(0f, 1f)] private float minimumUpwardNormal = 0.9f;
    [SerializeField] private LayerMask placementLayers = ~0;

    private WeightedObject carriedObject;

    public WeightedObject CarriedObject => carriedObject;

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (playerCamera == null || carryPoint == null)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            if (carriedObject == null)
            {
                TryPickUp();
            }
            else
            {
                TryPlaceCarriedObject();
            }
        }

        if (carriedObject != null)
        {
            carriedObject.transform.SetPositionAndRotation(carryPoint.position, carryPoint.rotation);
        }
    }

    // Attempts to pick up the closest weighted object in front of the player
    private void TryPickUp()
    {
        // Ensures physics data is up to date before raycasting
        Physics.SyncTransforms();

        // Creates a ray from the center of the screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Finds all valid objects within the interaction range
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange, interactionLayers, QueryTriggerInteraction.Ignore);

        // Sorts hits from nearest to furthest
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // Ignore the player's Colliders
            if (BelongsToPlayer(hit.collider))
            {
                continue;
            }

            // Find the WeightedObject attached to the hit object or its parent
            WeightedObject weight = hit.collider.GetComponentInParent<WeightedObject>();


            // Stop if the first valid hit is not a weighted object
            if (weight == null || weight.IsCarried)
            {
                return;
            }

            carriedObject = weight;
            carriedObject.PickUp();

            return;
        }
    }

    private void TryPlaceCarriedObject()
    {
        if (carriedObject == null)
        {
            return;
        }

        Physics.SyncTransforms();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Finds all potential placement surfaces within range
        RaycastHit[] hits = Physics.RaycastAll(ray, placementRange, placementLayers, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit surfaceHit = default;
        bool foundSurface = false;

        foreach (RaycastHit hit in hits)
        {
            // Ignore the player and the carried object
            if (BelongsToHeldObject(hit.collider) || BelongsToPlayer(hit.collider))
            {
                continue;
            }

            // Use the first valid surface hit
            surfaceHit = hit;
            foundSurface = true;
            break;
        }

        if (!foundSurface)
        {
            return;
        }

        // Checks whether the surface is mostly flat and facing upward
        float upwardAmount = Vector3.Dot(surfaceHit.normal, Vector3.up);

        if (upwardAmount < minimumUpwardNormal)
        {
            return;
        }

        if (!TryGetHeldBounds(out Bounds heldBounds))
        {
            return;
        }

        // Calculates the target position so that the object rests above the surface
        Vector3 targetBoundsCenter = surfaceHit.point + Vector3.up * (heldBounds.extents.y + surfaceGap);
        Vector3 positionOffset = targetBoundsCenter - heldBounds.center;
        Vector3 targetPosition = carriedObject.transform.position + positionOffset;

        // Uses a slightly smaller overlap box to avoid false positives
        Vector3 checkExtents = new Vector3(Mathf.Max(0.001f, heldBounds.extents.x - 0.005f), Mathf.Max(0.001f, heldBounds.extents.y - 0.005f), Mathf.Max(0.001f, heldBounds.extents.z - 0.005f));
        Collider[] blockers = Physics.OverlapBox(targetBoundsCenter, checkExtents, Quaternion.identity, placementLayers, QueryTriggerInteraction.Ignore);

        // Checks whether another object is blocking the placement area
        foreach (Collider blocker in blockers)
        {
            if (BelongsToHeldObject(blocker))
            {
                continue;
            }

            return;
        }

        WeightedObject placedObject = carriedObject;
        Rigidbody body = placedObject.GetComponent<Rigidbody>();

        placedObject.transform.position = targetPosition;

        if (body != null)
        {
            body.position = targetPosition;
        }

        // Makes the object upright and facing the player
        PlaceUprightFacingPlayer(placedObject.transform);

        // Drops the object and re-enables physics
        placedObject.Drop();
        
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        carriedObject = null;
    }

    private void PlaceUprightFacingPlayer(Transform objectTransform)
    {
        // Uses the player's forward direction flattened onto the ground plane
        Vector3 flatForward = playerCamera.transform.forward;
        flatForward.y = 0f;

        // Uses a default direction if the flattened direction is too small
        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();

        // Rotates the object to face the player while remaining upright
        objectTransform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    // Checks whether a Collider belongs to the currently carried object
    private bool BelongsToHeldObject(Collider other)
    {
        if (carriedObject == null)
        {
            return false;
        }

        Transform heldTransform = carriedObject.transform;

        return other.transform == heldTransform || other.transform.IsChildOf(heldTransform);
    }

    private bool BelongsToPlayer(Collider other)
    {
        if (playerRoot == null)
        {
            return false;
        }

        return other.transform == playerRoot || other.transform.IsChildOf(playerRoot);
    }

    // Calculates the combined bounds of all active, non-trigger Colliders on the carried object
    private bool TryGetHeldBounds(out Bounds bounds)
    {
        bounds = default;
        bool foundCollider = false;

        Collider[] objectColliders = carriedObject.GetComponentsInChildren<Collider>();

        foreach (Collider objectCollider in objectColliders)
        {
            if (!objectCollider.enabled || objectCollider.isTrigger || !objectCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            // Use the first valid Collider's bounds
            if (!foundCollider)
            {
                bounds = objectCollider.bounds;
                foundCollider = true;
            }
            else
            {
                // Combine bounds from multiple Colliders
                bounds.Encapsulate(objectCollider.bounds);
            }
        }

        return foundCollider;
    }
}
