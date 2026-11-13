using UnityEngine;

public class WireNode : MonoBehaviour
{
    public bool isSource;                 
    public WireNode partner;            
    public WireRod[] requiredRods; //Section 3

    [HideInInspector] public WireSection section;   
    [HideInInspector] public bool connected;
    [HideInInspector] public Wire wire;           


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