using System.Collections;
using UnityEngine;

public class PizzaOvenController : MonoBehaviour, IInteractable
{
    [Header("Cooking Settings")]
    [SerializeField] private float cookDuration = 10f;
    [SerializeField] private float maxEquipDistance = 3.0f;
    [SerializeField] private string uncookedItemID = "UncookedPizza";
    [SerializeField] private string promptText = "Warm up Pizza";

    [Header("Cooked Output")]
    [SerializeField] private GameObject cookedPizzaPrefab;

    [Header("Oven Effects")]
    [SerializeField] private Light ovenRealtimeLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cookingLoopSFX;
    [SerializeField] private AudioClip timerDoneSFX;

    private bool isCooking;
    private bool isCooked;

    private void Awake()
    {
        if (ovenRealtimeLight != null) ovenRealtimeLight.enabled = false;
    }

    public void Interact()
    {
        if (isCooking || isCooked) return;

        FPSController player = FindFirstObjectByType<FPSController>();
        if (player == null) return;

        PlayerInteraction playerInteraction = player.GetComponent<PlayerInteraction>();
        if (playerInteraction == null) return;

        ObjectGrabbable heldItem = playerInteraction.CurrentGrabbable;

        if (heldItem != null && heldItem.ItemID == uncookedItemID)
        {
            playerInteraction.ClearCurrentGrabbableForSnap(transform, true);
            StartCoroutine(CookPizzaRoutine(player, playerInteraction));
        }
    }

    private IEnumerator CookPizzaRoutine(FPSController player, PlayerInteraction playerInteraction)
    {
        isCooking = true;

        if (ovenRealtimeLight != null) ovenRealtimeLight.enabled = true;

        if (audioSource != null && cookingLoopSFX != null)
        {
            audioSource.clip = cookingLoopSFX;
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(cookDuration);

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            if (timerDoneSFX != null)
            {
                audioSource.PlayOneShot(timerDoneSFX);
            }
        }

        if (ovenRealtimeLight != null) ovenRealtimeLight.enabled = false;

        while (player != null && Vector3.Distance(player.transform.position, transform.position) > maxEquipDistance)
        {
            yield return null;
        }

        if (cookedPizzaPrefab != null && playerInteraction != null)
        {
            GameObject newCookedPizza = Instantiate(cookedPizzaPrefab);
            ObjectGrabbable grabbable = newCookedPizza.GetComponent<ObjectGrabbable>();

            if (grabbable != null)
            {
                playerInteraction.ForceEquipGrabbable(grabbable);
            }
        }

        isCooking = false;
        isCooked = true;
    }

    public bool CanInteract(ObjectGrabbable heldItem)
    {
        return !isCooking && !isCooked && heldItem != null && heldItem.ItemID == uncookedItemID;
    }

    public string GetPromptMessage(ObjectGrabbable heldItem)
    {
        if (isCooking) return "Cooking pizza...";
        if (isCooked) return string.Empty;
        if (CanInteract(heldItem)) return promptText;
        return string.Empty;
    }
}