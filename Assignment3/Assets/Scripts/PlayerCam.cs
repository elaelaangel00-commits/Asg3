
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;

    float xRotation;
    float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Get mouse input using Unity 6 Input System
        Vector2 mouseInput = Mouse.current.delta.ReadValue();

        float mouseX = mouseInput.x * sensX;
        float mouseY = mouseInput.y * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;

        // Clamp vertical camera rotation
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Rotate camera and orientation
        transform.rotation = Quaternion.Euler(
            xRotation, yRotation, 0f
        );

        orientation.rotation = Quaternion.Euler(
            0f, yRotation, 0f
        );
    }
}
