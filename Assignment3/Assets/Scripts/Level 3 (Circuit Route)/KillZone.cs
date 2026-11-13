using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    public Transform sectionStart; //respawn spot

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerWireHand hand = other.GetComponentInParent<PlayerWireHand>();
        if (hand == null || sectionStart == null) return;

        Level3AudioManager.Instance?.PlaySFX(Level3AudioManager.Instance.Fall);

        if (hand.IsHolding)
        {
            hand.DropHeldPlug();   // also plays the Return Wire sound
            Level3UIManager.Message("You fell! The wire went back to its source", Color.yellow);
            Level3AudioManager.Instance?.PlaySFX(Level3AudioManager.Instance.ReturnWire);
        }
        else
        {
            Level3UIManager.Message("Oops, you fell!", Color.yellow);
        }

        Teleport(hand.transform, sectionStart.position);
    }

    static void Teleport(Transform player, Vector3 position)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (cc != null) cc.enabled = false;
        player.position = position;
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector3.zero;
        }
        if (cc != null) cc.enabled = true;
    }
}