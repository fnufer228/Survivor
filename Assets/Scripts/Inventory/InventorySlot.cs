using System;

[Serializable]
public class InventorySlot
{
    public ResourceType item;
    public int amount;

    public InventorySlot(ResourceType item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}