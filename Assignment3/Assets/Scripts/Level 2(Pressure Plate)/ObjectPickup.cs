using UnityEngine;

public class ObjectPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform carryPoint;
    [SerializeField] private Transform playerRoot;

    [Header("Pickup")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Crosshair Placement")]
    [SerializeField, Min(0.1f)] private float placementRange = 3f;
    [SerializeField, Min(0f)] private float surfaceGap = 0.02f;
    [SerializeField, Range(0f, 1f)] private float minimumUpwardNormal = 0.9f;
    [SerializeField] private LayerMask placementLayers = ~0;

    [Header("Audio")]
    [SerializeField] private AudioSource pickupAudio;
    [SerializeField] private AudioSource dropAudio;

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

    private void TryPickUp()
    {
        Physics.SyncTransforms();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange, interactionLayers, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (BelongsToPlayer(hit.collider))
            {
                continue;
            }

            WeightedObject weight = hit.collider.GetComponentInParent<WeightedObject>();

            if (weight == null || weight.IsCarried)
            {
                return;
            }

            carriedObject = weight;
            carriedObject.PickUp();

            if (pickupAudio != null)
            {
                pickupAudio.Play();
            }

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

        RaycastHit[] hits = Physics.RaycastAll(ray, placementRange, placementLayers, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit surfaceHit = default;
        bool foundSurface = false;

        foreach (RaycastHit hit in hits)
        {
            if (BelongsToHeldObject(hit.collider) || BelongsToPlayer(hit.collider))
            {
                continue;
            }

            surfaceHit = hit;
            foundSurface = true;
            break;
        }

        if (!foundSurface)
        {
            return;
        }

        float upwardAmount = Vector3.Dot(surfaceHit.normal, Vector3.up);

        if (upwardAmount < minimumUpwardNormal)
        {
            return;
        }

        if (!TryGetHeldBounds(out Bounds heldBounds))
        {
            return;
        }

        Vector3 targetBoundsCenter = surfaceHit.point + Vector3.up * (heldBounds.extents.y + surfaceGap);
        Vector3 positionOffset = targetBoundsCenter - heldBounds.center;
        Vector3 targetPosition = carriedObject.transform.position + positionOffset;

        Vector3 checkExtents = new Vector3(Mathf.Max(0.001f, heldBounds.extents.x - 0.005f), Mathf.Max(0.001f, heldBounds.extents.y - 0.005f), Mathf.Max(0.001f, heldBounds.extents.z - 0.005f));
        Collider[] blockers = Physics.OverlapBox(targetBoundsCenter, checkExtents, Quaternion.identity, placementLayers, QueryTriggerInteraction.Ignore);

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

        placedObject.Drop();
        
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        carriedObject = null;

        if (dropAudio != null)
        {
            dropAudio.Play();
        }
    }

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

            if (!foundCollider)
            {
                bounds = objectCollider.bounds;
                foundCollider = true;
            }
            else
            {
                bounds.Encapsulate(objectCollider.bounds);
            }
        }

        return foundCollider;
    }
}
