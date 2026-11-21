using UnityEngine;

[CreateAssetMenu(menuName = "Game/Build Resource")]
public class BuildResource : ScriptableObject
{
    public string id = "log";
    public string displayName = "Log";
    public Sprite icon;
    public ResourceType inventoryItem;
    public PickupItemType dropItem;

    [Tooltip("Carried in hands (logs) instead of stored in the inventory (sticks, stones)")]
    public bool carriedInHands;
}
