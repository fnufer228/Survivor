using Photon.Pun;
using UnityEngine;

/// <summary>
/// Простий FPS-рух через CharacterController: WASD + миша + стрибок, без присідання/спринту
/// (як в Overwatch). Керує тільки локальний гравець — інші клієнти бачать рух через
/// звичайну синхронізацію Transform, а анімацію third-person читає PlayerVisuals.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Дочірній transform камери — нахиляється вгору/вниз, тіло гравця обертається окремо (лише по Y)")]
    public Transform playerCamera;

    [Tooltip("Камера рук (Overlay, малює лише шар Weapon). У чужих гравців вимикається разом з руками")]
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
        // Камера/аудіо-слухач вмикаються лише в локального гравця — інакше на екрані
        // буде по камері на кожного гравця в кімнаті
        bool isMine = _photonView.IsMine;

        if (playerCamera != null)
        {
            // GetComponentsInChildren (множина!) — камер може бути дві: основна
            // і окрема "камера зброї" (Depth Only, малює лише шар зброї, щоб не
            // клипало у стіни). Якщо лишити ввімкненою хоч одну чужу — вона малює
            // без перевірки глибини і "просвічує" крізь усе на екрані.
            foreach (Camera cam in playerCamera.GetComponentsInChildren<Camera>(true))
                cam.enabled = isMine;

            foreach (AudioListener listener in playerCamera.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = isMine;
        }

        // Руки чужого гравця лежать на шарі Weapon — без цього наша камера рук малювала б їх поверх усього
        if (weaponCamera != null && !isMine)
            weaponCamera.SetActive(false);

        if (!isMine) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        if (!_photonView.IsMine) return;

        Look();
        Move();
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

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized * moveSpeed;

        if (_controller.isGrounded)
        {
            _verticalVelocity = -1f; // невеликий "притиск" до землі, щоб isGrounded не мигав

            if (Input.GetButtonDown("Jump"))
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
