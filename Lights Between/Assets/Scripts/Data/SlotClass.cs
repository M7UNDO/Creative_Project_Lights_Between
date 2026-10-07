using System;
using UnityEngine;

[Serializable]
public class SlotClass
{
    [field: SerializeField] public ItemClass Item { get; private set; }
    [field: SerializeField] public int Quantity { get; private set; }

    public SlotClass()
    {
        Item = null;
        Quantity = 0;
    }

    public SlotClass(ItemClass item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public void AddQuantity(int amount)
    {
        Quantity += amount;
    }

    public void SubQuantity(int amount)
    {
        Quantity -= amount;
        if (Quantity <= 0)
        {
            Clear();
        }
    }

    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }
}