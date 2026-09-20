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

    [Header("Completion Cleanup")]
    [Tooltip("If true, destroys this GameObject when complete. If false, disables the collider and script instead.")]
    [SerializeField] private bool destroySnapZoneOnComplete = false;

    [Header("Events & Audio")]
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string interactionEventID = InteractionIDs.PackBags;
    [SerializeField] private AudioClip snapSFX;

    [Header("Invalid Item Dialogue")]
    [SerializeField] private string invalidItemID = "UncookedPizza";
    [SerializeField] private DialogueSO invalidItemDialogue;

    [Header("Dialogue Trigger")]
    [SerializeField] private DialogueSO postSnapDialogue;

    private int currentSnappedCount = 0;

    public void Interact()
    {
        FPSController player = FindFirstObjectByType<FPSController>();
        if (player == null) return;

        ObjectGrabbable heldItem = player.CurrentGrabbable;

        if (heldItem != null && heldItem.ItemID == invalidItemID)
        {
            if (DialogueManager.Instance != null && invalidItemDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(invalidItemDialogue);
            }
            return;
        }

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

                CleanupSnapZone();
            }
        }
    }

    private void CleanupSnapZone()
    {
        if (destroySnapZoneOnComplete)
        {
            Destroy(gameObject);
        }
        else
        {
            if (TryGetComponent<Collider>(out Collider col))
            {
                col.enabled = false;
            }

            enabled = false;
        }
    }

    public bool CanSnap(ObjectGrabbable heldItem)
    {
        if (currentSnappedCount >= requiredItemCount) return false;

        if (heldItem != null && heldItem.ItemID == invalidItemID) return true;

        return heldItem != null && heldItem.ItemID == requiredItemID;
    }

    public string GetPromptMessage(ObjectGrabbable heldItem)
    {
        if (currentSnappedCount >= requiredItemCount) return string.Empty;

        if (heldItem != null && heldItem.ItemID == invalidItemID)
        {
            return "Place Pizza";
        }

        if (CanSnap(heldItem))
        {
            return $"{promptText} ({currentSnappedCount}/{requiredItemCount})";
        }

        return string.Empty;
    }
}