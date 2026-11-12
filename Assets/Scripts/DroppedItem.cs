using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    public int Id { get; private set; }
    public int TypeIndex { get; private set; }
    public int SpawnTime { get; private set; }
    public PickupItemType Type { get; private set; }

    public string Prompt => Type != null ? Type.displayName : string.Empty;
    public bool CanInteract => Type != null;

    public void Init(int id, PickupItemType type, int typeIndex, int spawnTime)
    {
        Id = id;
        Type = type;
        TypeIndex = typeIndex;
        SpawnTime = spawnTime;
    }

    public void Interact()
    {
        if (DropManager.Instance != null) DropManager.Instance.Pickup(this);
    }
}
