
using UnityEngine;

public class MirrorButton : MonoBehaviour
{
    public Transform mirror;
    public float rotateAngle = 45f;

    public void RotateMirror()
    {
        //rotate mirror when button is pressed
        mirror.Rotate(0f, rotateAngle, 0f, Space.World);
    }
}
