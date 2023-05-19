using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float movementSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 5f;
    private float verticalRotation = 0f;
    private CharacterController characterController;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock the cursor to the game window and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Player movement
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 movement = transform.forward * moveVertical + transform.right * moveHorizontal;

        if (characterController.isGrounded == false)
        {
            movement += Physics.gravity;
        }

        characterController.Move(movement * movementSpeed * Time.deltaTime);

        // Player rotation
        float mouseHorizontal = Input.GetAxis("Mouse X");
        float mouseVertical = Input.GetAxis("Mouse Y");

        transform.Rotate(Vector3.up * mouseHorizontal * mouseSensitivity);

        verticalRotation -= mouseVertical * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        Camera.main.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}
