using System.Collections.Generic;
using UnityEngine;

public class PlayerToolInventory : MonoBehaviour
{
    [Header("Settings", order = 0)]
    [Space(5)]
    [SerializeField] private List<ToolSlotData> acquiredTools = new List<ToolSlotData>();

    public List<ToolSlotData> AcquiredTools => acquiredTools;

    public void AddTool(ToolClass tool, int quantity = 1)
    {
        if (tool == null) return;

        if (tool.isStackable)
        {
            foreach (ToolSlotData slot in acquiredTools)
            {
                if (slot.Tool == tool && slot.Quantity < tool.stackSize)
                {
                    int spaceAvailable = tool.stackSize - slot.Quantity;
                    int amountToAdd = Mathf.Min(quantity, spaceAvailable);
                    slot.AddQuantity(amountToAdd);
                    quantity -= amountToAdd;

                    if (quantity <= 0) return;
                }
            }
        }

        while (quantity > 0)
        {
            int amountToAdd = tool.isStackable ? Mathf.Min(quantity, tool.stackSize) : 1;
            acquiredTools.Add(new ToolSlotData(tool, amountToAdd));
            quantity -= amountToAdd;
        }
    }

    public int GetToolQuantity(ToolClass tool)
    {
        if (tool == null) return 0;

        int total = 0;
        foreach (ToolSlotData slot in acquiredTools)
        {
            if (slot.Tool == tool)
            {
                total += slot.Quantity;
            }
        }

        return total;
    }

    public ToolClass GetToolByType(ToolType typeToGet)
    {
        ToolSlotData slot = acquiredTools.Find(s => s != null && s.Tool != null && s.Tool.toolType == typeToGet);
        return slot?.Tool;
    }

    public bool HasToolType(ToolType typeToCheck)
    {
        return acquiredTools.Exists(s => s != null && s.Tool != null && s.Tool.toolType == typeToCheck);
    }

    public bool RemoveTool(ToolClass tool, int quantity = 1)
    {
        if (tool == null) return false;

        for (int i = acquiredTools.Count - 1; i >= 0; i--)
        {
            if (acquiredTools[i].Tool == tool)
            {
                if (acquiredTools[i].Quantity > quantity)
                {
                    acquiredTools[i].SubQuantity(quantity);
                    return true;
                }
                else
                {
                    quantity -= acquiredTools[i].Quantity;
                    acquiredTools.RemoveAt(i);
                    if (quantity <= 0) return true;
                }
            }
        }

        return false;
    }

    public bool HasTool(ToolClass tool)
    {
        return acquiredTools.Exists(s => s != null && s.Tool == tool);
    }
}