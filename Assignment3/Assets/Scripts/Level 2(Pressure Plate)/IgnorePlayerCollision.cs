using UnityEngine;

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

                if (objectCollider == playerCollider)
                {
                    continue;
                }

                Physics.IgnoreCollision(objectCollider, playerCollider, ignore);
            }
        }

        collisionsIgnored = ignore;
    }
}
