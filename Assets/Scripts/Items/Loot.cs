public static class Loot
{
    public static event System.Action<ResourceType, int> LocalReceived;

    public static bool GiveLocal(ResourceType item, int amount)
    {
        if (item == null || amount <= 0 || InventoryManager.Instance == null) return false;
        if (!InventoryManager.Instance.AddItem(item, amount)) return false;
        LocalReceived?.Invoke(item, amount);
        return true;
    }
}
