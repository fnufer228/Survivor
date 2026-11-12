using Photon.Pun;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventoryCamera;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private KeyCode inventoryKey = KeyCode.Tab;

    private bool isOpen;
    private PlayerMovement localPlayer;

    private void Start()
    {
        SetInventoryState(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        SetInventoryState(!isOpen);
    }

    public void SetInventoryState(bool state)
    {
        isOpen = state;

        Cursor.visible = state;
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;

        ResolveLocalPlayer();

        if (localPlayer != null)
            localPlayer.enabled = !state;

        if (inventoryCamera != null)
            inventoryCamera.SetActive(state);

        if (playerCamera != null)
            playerCamera.SetActive(!state);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RefreshInventory();
    }

    private void ResolveLocalPlayer()
    {
        if (localPlayer != null && playerCamera != null)
            return;

        foreach (PlayerMovement pm in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            PhotonView view = pm.GetComponent<PhotonView>();
            if (view == null || !view.IsMine)
                continue;

            localPlayer = pm;
            if (pm.playerCamera != null)
                playerCamera = pm.playerCamera.gameObject;
            return;
        }
    }
}