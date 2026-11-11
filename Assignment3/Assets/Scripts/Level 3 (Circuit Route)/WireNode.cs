using UnityEngine;

// Put this on every source cylinder and every socket cylinder.
// Each node must be a CHILD of the WireSection it belongs to (the section finds them automatically).
public class WireNode : MonoBehaviour
{
    public bool isSource;                 // tick for sources, untick for sockets (incl. decoy sockets)
    public WireNode partner;              // SOURCE only: drag its matching socket here
    public WireRod[] requiredRods;        // SOURCE only (Section 3): rods this wire must wrap around before plugging in

    [HideInInspector] public WireSection section;   // set automatically by the parent WireSection
    [HideInInspector] public bool connected;
    [HideInInspector] public Wire wire;            // SOURCE only: the wire currently coming out of it

    // Centre of the cylinder's top face - where wires start and plugs sit
    public Vector3 TopPoint
    {
        get
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Bounds b = col.bounds;
                return new Vector3(b.center.x, b.max.y, b.center.z);
            }
            return transform.position + Vector3.up * transform.lossyScale.y;
        }
    }
}