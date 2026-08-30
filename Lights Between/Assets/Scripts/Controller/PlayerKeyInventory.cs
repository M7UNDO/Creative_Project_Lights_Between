using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerKeyInventory : MonoBehaviour, IKeyInventory
{
    [SerializeField] private List<string> keys = new List<string>();
    [SerializeField] private float displayTime = 1.5f;

    [Header("UI")]
    public GameObject KeyCollectionContainer;
    public TextMeshProUGUI keyCollectedTxt;

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
            return true;
        }
        return false;
    }

    public void AddKey(string keyID)
    {
        if (!keys.Contains(keyID))
        {
            keys.Add(keyID);

            if (KeyCollectionContainer == null) return;
            StartCoroutine(DisplayKeyCollected(keyID));
        }
    }

    IEnumerator DisplayKeyCollected(string keyID)
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