using UnityEngine;

public class KeyPickUp : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private ItemClass keyItemData;

    public string KeyID = "Garage Key";
    public ObjectiveSO objectiveSO;

    [Header("SFX")]

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
            keyInventory.AddKey(KeyID);

            InventoryManager inventory = FindFirstObjectByType<InventoryManager>();

            if (inventory != null && keyItemData != null)
            {
                if (inventory.AddItem(keyItemData, 1))
                {
                    if (pickUpSFX != null)
                    {
                        AudioSource.PlayClipAtPoint(pickUpSFX, transform.position);
                    }
                }

                if (objectiveSO != null)
                {
                    if (ObjectiveManager.Instance != null)
                    {
                        ObjectiveManager.Instance.CompleteObjective(objectiveSO.ID);
                    }
                }

                Destroy(gameObject);
            }

            
        }
    }
}
