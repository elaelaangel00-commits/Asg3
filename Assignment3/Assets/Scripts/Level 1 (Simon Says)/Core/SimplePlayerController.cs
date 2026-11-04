using UnityEngine;

namespace Backrooms.Core
{
    /// <summary>
    /// TEST PLAYER ONLY. Lets you walk around and test Room 1 before the team's real
    /// player controller is ready. Swap it for the team's controller when it exists.
    /// WASD moves relative to where you face, diagonals are not faster, Space jumps.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;

            // Mouse look: body turns left/right, camera tilts up/down
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            transform.Rotate(0f, mouseX, 0f);
            pitch = Mathf.Clamp(pitch - mouseY, -85f, 85f);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);

            // Movement relative to facing. ClampMagnitude stops diagonals being faster.
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
            Vector3 move = transform.TransformDirection(input) * moveSpeed;

            // Gravity and jump
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (controller.isGrounded && Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }
    }
}
