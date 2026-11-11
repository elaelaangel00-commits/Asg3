using UnityEngine;

// Put this on each rod (an upright cylinder with a collider) in Section 3.
// While holding a plug, look at a rod and press E to wrap the wire around it.
// Paint the rod the same colour as the wire that needs it, and add it to that
// source's "Required Rods" list.
public class WireRod : MonoBehaviour
{
    public Transform wrapPoint;      // optional: where on the rod the wire wraps (default = middle of the rod)

    public Vector3 WrapPoint
    {
        get
        {
            if (wrapPoint != null) return wrapPoint.position;
            Collider col = GetComponent<Collider>();
            return col != null ? col.bounds.center : transform.position;
        }
    }

    public float Radius
    {
        get
        {
            Collider col = GetComponent<Collider>();
            return col != null ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 0.05f;
        }
    }
}