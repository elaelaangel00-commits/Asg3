using UnityEngine;

public class ObjectPickup : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform carryPoint;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private AudioSource pickupAudio;
    [SerializeField] private AudioSource dropAudio;

    private WeightedObject carriedObject;

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (carriedObject == null)
            {
                TryPickUp();
            }
            else
            {
                DropCarriedObject();
            }
        }

        if (carriedObject != null)
        {
            carriedObject.transform.SetPositionAndRotation(carryPoint.position, carryPoint.rotation);
        }
    }

    private void TryPickUp()
    {
        Ray interactionRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(interactionRay, out RaycastHit hit, interactionRange))
        {
            return;
        }

        if (!hit.collider.TryGetComponent(out WeightedObject weightedObject))
        {
            return;
        }

        carriedObject = weightedObject;
        carriedObject.PickUp();

        if (pickupAudio != null)
        {
            pickupAudio.Play();
        }
    }

    private void DropCarriedObject()
    {
        carriedObject.Drop();
        carriedObject = null;

        if (dropAudio != null)
        {
            dropAudio.Play();
        }
    }
}
