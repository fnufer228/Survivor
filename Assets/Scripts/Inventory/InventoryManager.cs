using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private List<InventorySlot> items = new List<InventorySlot>();
    [SerializeField] private List<InventoryZone> zones = new List<InventoryZone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshInventory();
    }

    public bool AddItem(ResourceType item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (item.maxAmount > 0)
        {
            int free = item.maxAmount - GetItemAmount(item);
            if (free <= 0)
                return false;
            amount = Mathf.Min(amount, free);
        }

        int remaining = amount;

        foreach (InventorySlot slot in items)
        {
            if (slot.item != item)
                continue;

            int space = item.maxStack - slot.amount;

            if (space <= 0)
                continue;

            int added = Mathf.Min(space, remaining);
            slot.amount += added;
            remaining -= added;

            if (remaining <= 0)
            {
                RefreshInventory();
                return true;
            }
        }

        while (remaining > 0)
        {
            int added = Mathf.Min(item.maxStack, remaining);
            items.Add(new InventorySlot(item, added));
            remaining -= added;
        }

        RefreshInventory();
        return true;
    }

    public bool RemoveItem(ResourceType item, int amount = 1)
    {
        if (!HasItem(item, amount))
            return false;

        int remaining = amount;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].item != item)
                continue;

            int removed = Mathf.Min(items[i].amount, remaining);
            items[i].amount -= removed;
            remaining -= removed;

            if (items[i].amount <= 0)
                items.RemoveAt(i);

            if (remaining <= 0)
                break;
        }

        RefreshInventory();
        return true;
    }

    public bool HasItem(ResourceType item, int amount = 1)
    {
        return GetItemAmount(item) >= amount;
    }

    public int GetItemAmount(ResourceType item)
    {
        int amount = 0;

        foreach (InventorySlot slot in items)
        {
            if (slot.item == item)
                amount += slot.amount;
        }

        return amount;
    }

    public InventorySlot GetSlot(ResourceType item)
    {
        foreach (InventorySlot slot in items)
        {
            if (slot.item == item)
                return slot;
        }

        return null;
    }

    public void RefreshInventory()
    {
        foreach (InventoryZone zone in zones)
            zone.Refresh();
    }

    public List<InventorySlot> GetItems()
    {
        return items;
    }
}