using System.Collections.Generic;
using UnityEngine;

public class PlayerKeyInventory : MonoBehaviour, IKeyInventory
{
    [SerializeField] private List<string> keys = new List<string>();
    [SerializeField] private float displayTime = 1.5f;

    private readonly Dictionary<string, ItemClass> keyItemMap = new Dictionary<string, ItemClass>();

    public bool HasKey(string keyID)
    {
        return string.IsNullOrEmpty(keyID) || keys.Contains(keyID);
    }

    public bool ConsumeKey(string keyID)
    {
        if (HasKey(keyID))
        {
            keys.Remove(keyID);

            if (keyItemMap.TryGetValue(keyID, out ItemClass itemData))
            {
                InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
                if (inventory != null)
                {
                    inventory.RemoveItem(itemData, 1);
                }
                keyItemMap.Remove(keyID);
            }

            return true;
        }
        return false;
    }

    public void AddKey(string keyID, ItemClass itemData = null)
    {
        if (!keys.Contains(keyID))
        {
            keys.Add(keyID);

            if (itemData != null && !keyItemMap.ContainsKey(keyID))
            {
                keyItemMap.Add(keyID, itemData);
            }
        }
    }
}