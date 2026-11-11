using UnityEngine;

// Section 3 obby: put this on a big trigger box under the platforms (the water).
// Falling in drops the plug you're holding (the wire goes back to its source)
// and sends you back to the start of the section. Wires already plugged in stay connected.
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    public Transform sectionStart;   // where the player respawns (S3_Start)

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerWireHand hand = other.GetComponentInParent<PlayerWireHand>();
        if (hand == null || sectionStart == null) return;

        hand.DropHeldPlug();
        Level3AudioManager.Fall();
        Teleport(hand.transform, sectionStart.position);
        Level3UIManager.Message("You fell! The wire went back to its source", Color.yellow);
    }

    static void Teleport(Transform player, Vector3 position)
    {
        // works for both CharacterController and Rigidbody players
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