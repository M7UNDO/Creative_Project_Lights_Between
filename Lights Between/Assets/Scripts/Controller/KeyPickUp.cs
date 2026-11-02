using UnityEngine;

public class KeyPickUp : MonoBehaviour, IInteractable
{
    [Header("References")]
    [Space(5)]
    [SerializeField] private ItemClass keyItemData;
    [SerializeField] private string promptMessage = "Pick up";

    [Header("Key Settings")]
    [Space(5)]
    public string KeyID = "Garage Key";
    public ObjectiveSO objectiveSO;

    [Header("SFX")]
    [Space(5)]
    public AudioClip pickUpSFX;

    public string PromptMessage => promptMessage;

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