
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;
    public Camera viewCamera;

    [Tooltip("False = look with crosshair. True = hover mouse.")]
    public bool useMouseHover = false;

    [Header("Interaction UI")]
    public TMP_Text interactionPrompt;

    [Tooltip("Enable only for a world-space prompt.")]
    public bool movePromptAboveButton = false;

    public float promptHeight = 0.7f;

    private void Start()
    {
        if (viewCamera == null)
            viewCamera = GetComponent<Camera>();

        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();

        if (viewCamera == null)
            viewCamera = Camera.main;

        HidePrompt();
    }

    private void Update()
    {
        MirrorButton button = FindButton();

        if (button == null)
        {
            HidePrompt();
            return;
        }

        ShowPrompt(button);

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            button.Interact();
            ShowPrompt(button);
        }
    }

    private MirrorButton FindButton()
    {
        if (viewCamera == null)
            return null;

        Ray ray;

        if (useMouseHover && Mouse.current != null)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            ray = viewCamera.ScreenPointToRay(
                mousePosition
            );
        }
        else
        {
            ray = new Ray(
                viewCamera.transform.position,
                viewCamera.transform.forward
            );
        }

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide))
        {
            return hit.collider
                .GetComponentInParent<MirrorButton>();
        }

        return null;
    }

    private void ShowPrompt(MirrorButton button)
    {
        if (interactionPrompt == null)
            return;

        interactionPrompt.gameObject.SetActive(true);
        interactionPrompt.text = button.PromptText;

        // Use this only for World Space Canvas.
        if (movePromptAboveButton)
        {
            interactionPrompt.transform.position =
                button.transform.position +
                Vector3.up * promptHeight;

            if (viewCamera != null)
            {
                interactionPrompt.transform.rotation =
                    viewCamera.transform.rotation;
            }
        }
    }

    private void HidePrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);
    }
}
