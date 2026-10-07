using UnityEngine;

public class BatteryPickUp : MonoBehaviour, IInteractable
{
    [Header("Item Configuration", order = 0)]
    [Space(5)]
    [SerializeField] private ItemClass batteryItemData;

    [Header("SFX", order = 1)]
    [Space(5)]
    [SerializeField] private AudioClip pickUpSFX;

    public void Interact()
    {
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();

        if (inventory != null && batteryItemData != null)
        {
            if (inventory.AddItem(batteryItemData, 1))
            {
                if (pickUpSFX != null)
                {
                    AudioSource.PlayClipAtPoint(pickUpSFX, transform.position);
                }
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}