using System.Collections.Generic;
using UnityEngine;

public class SnapZone : MonoBehaviour, IInteractable
{
    [Header("Requirements")]
    [SerializeField] private string requiredItemID = "Bag";
    [SerializeField] private int requiredItemCount = 2;
    [SerializeField] private string promptText = "Place Bag";

    [Header("Snap Behavior")]
    [SerializeField] private List<Transform> snapPoints = new List<Transform>();
    [SerializeField] private bool destroyObjectOnSnap = false;
    [SerializeField] private GameObject enableVisualOnSnap;

    [Header("Events & Audio")]
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string interactionEventID = InteractionIDs.PackBags;
    [SerializeField] private AudioClip snapSFX;

    [Header("Dialogue Trigger")]
    [SerializeField] private DialogueSO postSnapDialogue;

    private int currentSnappedCount = 0;

    public void Interact()
    {
        FPSController player = FindFirstObjectByType<FPSController>();
        if (player == null) return;

        ObjectGrabbable heldItem = player.CurrentGrabbable;

        if (heldItem != null && heldItem.ItemID == requiredItemID)
        {
            Transform snapTarget = snapPoints.Count > currentSnappedCount ? snapPoints[currentSnappedCount] : transform;

            player.ClearCurrentGrabbableForSnap(snapTarget, destroyObjectOnSnap);

            currentSnappedCount++;

            if (enableVisualOnSnap != null)
            {
                enableVisualOnSnap.SetActive(true);
            }

            if (snapSFX != null)
            {
                AudioSource.PlayClipAtPoint(snapSFX, transform.position);
            }

            if (currentSnappedCount >= requiredItemCount)
            {
                if (interactionChannel != null)
                {
                    interactionChannel.Raise(interactionEventID);
                }

                if (DialogueManager.Instance != null && postSnapDialogue != null)
                {
                    DialogueManager.Instance.StartDialogue(postSnapDialogue);
                }
            }
        }
    }

    public bool CanSnap(ObjectGrabbable heldItem)
    {
        return heldItem != null && heldItem.ItemID == requiredItemID && currentSnappedCount < requiredItemCount;
    }

    public string GetPromptMessage(ObjectGrabbable heldItem)
    {
        if (CanSnap(heldItem))
        {
            return $"{promptText} ({currentSnappedCount}/{requiredItemCount})";
        }
        return string.Empty;
    }
}