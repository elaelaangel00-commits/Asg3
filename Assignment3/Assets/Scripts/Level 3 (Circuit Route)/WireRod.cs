using UnityEngine;

public class WireRod : MonoBehaviour
{
    public Transform wrapPoint;

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