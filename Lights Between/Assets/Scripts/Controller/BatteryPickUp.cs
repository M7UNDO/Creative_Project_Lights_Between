using UnityEngine;

public class BatteryPickUp : MonoBehaviour, IInteractable
{
    [Header("SFX")]
    [SerializeField] private AudioClip pickUpSFX;

    public void Interact()
    {
        FlashlightScript flashlight = FindFirstObjectByType<FlashlightScript>(FindObjectsInactive.Include);
        if (flashlight != null)
        {
            if (flashlight.TryAddBattery())
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