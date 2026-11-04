using UnityEngine;

public class Level5Interact : MonoBehaviour
{
    [SerializeField] private Camera playerCam;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            KeyAndExitHandler handler = hit.collider.GetComponentInParent<KeyAndExitHandler>();
            if (handler != null) handler.Interact();
        }
    }
}