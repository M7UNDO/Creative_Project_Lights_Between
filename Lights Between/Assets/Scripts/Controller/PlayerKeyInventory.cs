using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerKeyInventory : MonoBehaviour, IKeyInventory
{
    [SerializeField] private List<string> keys = new List<string>();
    [SerializeField] private float displayTime = 1.5f;

    [Header("UI", order = 0)]
    [Space(5)]
    public GameObject KeyCollectionContainer;
    public TextMeshProUGUI keyCollectedTxt;

    private readonly Dictionary<string, ItemClass> keyItemMap = new Dictionary<string, ItemClass>();

    public void Start()
    {
        if (KeyCollectionContainer != null)
        {
            KeyCollectionContainer.SetActive(false);
        }
    }

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

            if (KeyCollectionContainer != null)
            {
                StartCoroutine(DisplayKeyCollected(keyID));
            }
        }
    }

    private IEnumerator DisplayKeyCollected(string keyID)
    {
        if (keyCollectedTxt != null)
        {
            string formattedKeyName = keyID.Replace("_", " ").Replace("-", " ");
            keyCollectedTxt.text = $"Collected: {formattedKeyName}";
        }

        KeyCollectionContainer.SetActive(true);

        yield return new WaitForSeconds(displayTime);
        KeyCollectionContainer.SetActive(false);
    }
}