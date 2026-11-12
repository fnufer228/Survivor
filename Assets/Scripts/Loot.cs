public static class Loot
{
    public static event System.Action<string, int> LocalReceived;

    public static void GiveLocal(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        LocalReceived?.Invoke(itemId, amount);
    }
}
