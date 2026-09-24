using UnityEngine;

public class KeyPickUp : MonoBehaviour, IInteractable
{
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

            if (pickUpSFX != null)
            {
                AudioSource.PlayClipAtPoint(pickUpSFX, transform.position);
            }

            if (objectiveSO != null)
            {
                if(ObjectiveManager.Instance != null)
                {
                    ObjectiveManager.Instance.CompleteObjective(objectiveSO.ID);
                }
            }

            Destroy(gameObject);
        }
    }
}
