using UnityEngine;

public class WireNode : MonoBehaviour
{
    public bool isSource;
    public WireNode partner;      // only needed on the SOURCE: drag its socket here
    public bool connected;
}