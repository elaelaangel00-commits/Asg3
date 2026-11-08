using UnityEngine;

public class WeightedObject : MonoBehaviour
{
    [SerializeField] private int weight = 1;

    [Header("Audio")]
    [SerializeField] private AudioSource objectAudio;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip placeClip;

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

        PlayPickupSound();

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.isKinematic = true;
            rigidbodyComponent.useGravity = false;
        }
    }

    public void Drop()
    {
        IsCarried = false;

        PlayPlaceSound();

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.isKinematic = false;
            rigidbodyComponent.useGravity = true;
        }
    }

    public void PlayPickupSound()
    {
        if (objectAudio != null && pickupClip != null)
        {
            objectAudio.PlayOneShot(pickupClip);
        }
    }

    public void PlayPlaceSound()
    {
        if (objectAudio != null && placeClip != null)
        {
            objectAudio.PlayOneShot(placeClip);
        }
    }
}
