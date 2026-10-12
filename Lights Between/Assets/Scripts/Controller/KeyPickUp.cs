using UnityEngine;

public class KeyPickUp : MonoBehaviour, IInteractable
{
    [Header("References", order = 0)]
    [Space(5)]
    [SerializeField] private ItemClass keyItemData;

    [Header("Key Settings", order = 1)]
    [Space(5)]
    public string KeyID = "Garage Key";
    public ObjectiveSO objectiveSO;

    [Header("SFX", order = 2)]
    [Space(5)]
    public AudioClip pickUpSFX;

    public void Interact()
    {
        PickupKey();
    }

    public void PickupKey()
    {
        GameObject player = GameObject.Find("Player");

        if (player != null && player.TryGetComponent<PlayerKeyInventory>(out PlayerKeyInventory keyInventory))
        {
            keyInventory.AddKey(KeyID, keyItemData);

            InventoryManager inventory = FindFirstObjectByType<InventoryManager>();

            if (inventory != null && keyItemData != null)
            {
                inventory.AddItem(keyItemData, 1);
            }

            if (pickUpSFX != null)
            {
                AudioSource.PlayClipAtPoint(pickUpSFX, transform.position);
            }

            if (objectiveSO != null && ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.CompleteObjective(objectiveSO.ID);
            }

            Destroy(gameObject);
        }
    }
}