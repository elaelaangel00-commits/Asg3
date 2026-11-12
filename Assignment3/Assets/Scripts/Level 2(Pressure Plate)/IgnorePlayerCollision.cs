using UnityEngine;

// Prevents a carried weight from colliding with the player
//Collision is automatically restored once the weight is placed
[RequireComponent(typeof(WeightedObject))]
public class IgnorePlayerCollision : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;

    private WeightedObject weightedObject;
    private Collider[] objectColliders;
    private Collider[] playerColliders;

    private bool collisionsIgnored;

    private void Start()
    {
        weightedObject = GetComponent<WeightedObject>();

        if (playerRoot == null)
        {
            enabled = false;
            return;
        }

        objectColliders = GetComponentsInChildren<Collider>(true);
        playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);

        SetIgnored(weightedObject.IsCarried);
    }

    private void FixedUpdate()
    {
        if (weightedObject.IsCarried != collisionsIgnored)
        {
            SetIgnored(weightedObject.IsCarried);
        }
    }

    // Enables or disables collision between the object and the player
    private void SetIgnored(bool ignore)
    {
        foreach (Collider objectCollider in objectColliders)
        {
            foreach (Collider playerCollider in playerColliders)
            {
                if (objectCollider == null || playerCollider == null)
                {
                    continue;
                }

                // Do not ignore collision between an object and itself
                if (objectCollider == playerCollider)
                {
                    continue;
                }

                // Only ignore collision while the object is being carried
                Physics.IgnoreCollision(objectCollider, playerCollider, ignore);
            }
        }

        collisionsIgnored = ignore;
    }
}
