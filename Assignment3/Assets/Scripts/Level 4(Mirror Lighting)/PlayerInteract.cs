
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 3f;

    private void Update()
    {
        //press E to interact with button
        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.forward, out hit, interactDistance))
            {
                MirrorButton button = hit.collider.GetComponent<MirrorButton>();

                if (button != null)
                    button.RotateMirror();
            }
        }
    }
}
