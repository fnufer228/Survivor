using Photon.Pun;
using UnityEngine;

public class PlayerHarvester : MonoBehaviourPun
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupDistance = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private LayerMask layer;

    private ResourceNode _currentItem;

    private void Update()
    {
        if (!photonView.IsMine) return;
        CheckForItem();

        if (_currentItem != null && Input.GetKeyDown(pickupKey))
            Pickup();
    }

    private void CheckForItem()
    {
        ResourceNode targetItem = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, layer))
        {
            Debug.Log(hit.transform.name);
            targetItem = hit.collider.GetComponentInParent<ResourceNode>();
        }
        else
        {
            Debug.Log("not Found");
        }
        if (targetItem == _currentItem)
            return;

        SetHighlight(_currentItem, false);
        _currentItem = targetItem;
        SetHighlight(_currentItem, true);
    }

    private static bool CanPickup(ResourceNode node)
    {
        if (node == null) return false;
        DroppedItem dropped = node.GetComponent<DroppedItem>();
        if (dropped != null) return dropped.CanInteract;
        return node.CanInteract;
    }

    private static void SetHighlight(ResourceNode node, bool on)
    {
        if (node == null) return;
        if (on && CanPickup(node)) InteractOutline.Show(node);
        else InteractOutline.Hide(node);
    }

    private void OnDisable()
    {
        SetHighlight(_currentItem, false);
        _currentItem = null;
    }

    private void Pickup()
    {
        if (_currentItem == null)
            return;

        ResourceType resourceType = _currentItem.GetItemData();

        DroppedItem dropped = _currentItem.GetComponent<DroppedItem>();
        if (dropped != null)
        {
            if (resourceType != null && InventoryManager.Instance.AddItem(resourceType))
            {
                dropped.Interact();
                SetHighlight(_currentItem, false);
                _currentItem = null;
            }
            return;
        }

        IInteractable interactable = _currentItem.GetComponent<IInteractable>();
        if (!interactable.CanInteract) return;
        interactable.Interact();

        if (resourceType == null)
            return;

        if (InventoryManager.Instance.AddItem(resourceType))
        {
            SetHighlight(_currentItem, false);
            _currentItem = null;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerCamera.transform.position, playerCamera.transform.position + playerCamera.transform.forward * pickupDistance);
    }
}