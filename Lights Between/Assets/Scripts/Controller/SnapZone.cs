using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SnapZone : MonoBehaviour, IInteractable
{
    [Header("Requirements")]
    [SerializeField] private string requiredItemID = "BreakerHandle";
    [SerializeField] private int requiredItemCount = 1;
    [SerializeField] private string promptText = "Attach Lever";

    [Header("Snap Behavior")]
    [SerializeField] private List<Transform> snapPoints = new List<Transform>();
    [SerializeField] private bool destroyObjectOnSnap = true;
    [SerializeField] private GameObject enableVisualOnSnap;

    [Header("Breaker Box Target")]
    [SerializeField] private ElectricBoxBreaker breakerBoxToRepair;

    [Header("Objective Progression")]
    [SerializeField] private ObjectiveSO performRitualObjective;

    [Header("Completion Cleanup")]
    [Tooltip("If true, destroys this GameObject when complete. If false, disables the collider and script instead.")]
    [SerializeField] private bool destroySnapZoneOnComplete = false;

    [Header("Events & Audio")]
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string interactionEventID = "BreakerRepaired";
    [SerializeField] private AudioClip snapSFX;

    [Header("Unity Event Hook")]
    [SerializeField] private UnityEvent onSnapComplete;

    [Header("Invalid Item Dialogue")]
    [SerializeField] private string invalidItemID = "UncookedPizza";
    [SerializeField] private DialogueSO invalidItemDialogue;

    [Header("Dialogue Trigger")]
    [SerializeField] private DialogueSO postSnapDialogue;

    private int currentSnappedCount = 0;
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    public void Interact()
    {
        if (!CanSnapForScenario()) return;

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

            if (breakerBoxToRepair != null)
            {
                breakerBoxToRepair.AttachMissingLever();
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

                if (ObjectiveManager.Instance != null && performRitualObjective != null)
                {
                    ObjectiveManager.Instance.ActivateObjective(performRitualObjective);
                }

                if (DialogueManager.Instance != null && postSnapDialogue != null)
                {
                    DialogueManager.Instance.StartDialogue(postSnapDialogue);
                }

                onSnapComplete?.Invoke();

                CleanupSnapZone();
            }
        }
    }

    private void CleanupSnapZone()
    {
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        if (destroySnapZoneOnComplete)
        {
            Destroy(gameObject);
        }
        else
        {
            enabled = false;
        }
    }

    private bool CanSnapForScenario()
    {
        if (breakerBoxToRepair != null)
        {
            return breakerBoxToRepair.IsLeverMissing;
        }
        return true;
    }

    public bool CanSnap(ObjectGrabbable heldItem)
    {
        if (!CanSnapForScenario() || currentSnappedCount >= requiredItemCount) return false;

        if (heldItem != null && heldItem.ItemID == invalidItemID) return true;

        return heldItem != null && heldItem.ItemID == requiredItemID;
    }

    public string GetPromptMessage(ObjectGrabbable heldItem)
    {
        if (!CanSnapForScenario() || currentSnappedCount >= requiredItemCount) return string.Empty;

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