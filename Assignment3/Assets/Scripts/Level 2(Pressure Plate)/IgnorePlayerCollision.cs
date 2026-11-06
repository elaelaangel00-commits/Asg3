using UnityEngine;

public class IgnorePlayerCollision : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;

    private void Start()
    {
        if (playerRoot == null)
        {
            return;
        }

        Collider[] objectColliders = GetComponentsInChildren<Collider>();

        Collider[] playerColliders = playerRoot.GetComponentsInChildren<Collider>();

        foreach (Collider objectCollider in objectColliders)
        {
            foreach (Collider playerCollider in playerColliders)
            {
                Physics.IgnoreCollision(objectCollider, playerCollider, true);
            }
        }
    }
}
