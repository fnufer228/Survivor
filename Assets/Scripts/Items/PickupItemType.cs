using UnityEngine;

[CreateAssetMenu(menuName = "Game/Pickup Item")]
public class PickupItemType : ScriptableObject
{
    public string itemId = "stick";
    public string displayName = "Stick";

    [Tooltip("Prefab WITHOUT PhotonView, with a collider")]
    public GameObject prefab;

    [Tooltip("Inventory item added on pickup. If empty, the Resource Type of the ResourceNode on the prefab is used")]
    public ResourceType inventoryItem;

    [Min(1)] public int amount = 1;

    public ResourceType ResolveInventoryItem()
    {
        if (inventoryItem != null) return inventoryItem;
        if (prefab != null && prefab.TryGetComponent(out ResourceNode node)) return node.GetItemData();
        return null;
    }
}
