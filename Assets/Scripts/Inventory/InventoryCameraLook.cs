using UnityEngine;

public class InventoryCameraLook : MonoBehaviour
{
    [SerializeField] private float maxHorizontalRotation;
    [SerializeField] private float maxVerticalRotation;
    [SerializeField] private float smoothSpeed;

    private Quaternion startRotation;

    private void Start()
    {
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        float mouseX = (Input.mousePosition.x / Screen.width) * 2f - 1f;
        float mouseY = (Input.mousePosition.y / Screen.height) * 2f - 1f;

        mouseX = Mathf.Clamp(mouseX, -1f, 1f);
        mouseY = Mathf.Clamp(mouseY, -1f, 1f);

        Quaternion targetRotation = startRotation * Quaternion.Euler(
            -mouseY * maxVerticalRotation,
            mouseX * maxHorizontalRotation,
            0f
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            smoothSpeed * Time.deltaTime
        );
    }
}