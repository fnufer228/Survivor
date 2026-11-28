using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Child camera transform. Pitches up/down, the body rotates only around Y")]
    public Transform playerCamera;

    [Tooltip("Hands camera (Overlay, renders only the Weapon layer). Disabled on remote players")]
    public GameObject weaponCamera;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 6f;
    public float gravity   = -20f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    CharacterController _controller;
    PhotonView _photonView;

    float _pitch;
    float _verticalVelocity;

    public bool IsGrounded => _controller.isGrounded;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        bool isMine = _photonView.IsMine;

        if (playerCamera != null)
        {
            foreach (Camera cam in playerCamera.GetComponentsInChildren<Camera>(true))
                cam.enabled = isMine;

            foreach (AudioListener listener in playerCamera.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = isMine;
        }
        if (!isMine) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        if (!_photonView.IsMine) return;

        bool frozen = RecipeBookUI.IsOpen;
        if (!frozen) Look();
        Move(frozen);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch = Mathf.Clamp(_pitch - mouseY, minPitch, maxPitch);
        if (playerCamera != null)
            playerCamera.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    void Move(bool frozen)
    {
        float x = frozen ? 0f : Input.GetAxisRaw("Horizontal");
        float z = frozen ? 0f : Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized * moveSpeed;

        if (_controller.isGrounded)
        {
            _verticalVelocity = -1f;

            if (!frozen && Input.GetButtonDown("Jump"))
                _verticalVelocity = jumpForce;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = _verticalVelocity;
        _controller.Move(move * Time.deltaTime);
    }
}
