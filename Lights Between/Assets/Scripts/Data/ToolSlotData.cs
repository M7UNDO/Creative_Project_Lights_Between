using System;
using UnityEngine;

[Serializable]
public class ToolSlotData
{
    [field: SerializeField] public ToolClass Tool { get; private set; }
    [field: SerializeField] public int Quantity { get; private set; }

    public ToolSlotData(ToolClass tool, int quantity)
    {
        Tool = tool;
        Quantity = quantity;
    }

    public void AddQuantity(int amount)
    {
        Quantity += amount;
    }

    public void SubQuantity(int amount)
    {
        Quantity -= amount;
        if (Quantity < 0)
        {
            Quantity = 0;
        }
    }
}