using UnityEngine;

public class WeightedObject : MonoBehaviour
{
    [SerializeField] private int weight = 1;

    private Rigidbody rigidbodyComponent;

    public int Weight => weight;
    public bool IsCarried { get; private set; }

    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
    }

    public void PickUp()
    {
        IsCarried = true;

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.isKinematic = true;
            rigidbodyComponent.useGravity = false;
        }
    }

    public void Drop()
    {
        IsCarried = false;

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.isKinematic = false;
            rigidbodyComponent.useGravity = true;
        }
    }
}
