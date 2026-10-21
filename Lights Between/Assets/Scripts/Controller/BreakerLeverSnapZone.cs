using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BreakerLeverSnapZone : MonoBehaviour, IInteractable
{
    [Header("Breaker Reference")]
    [SerializeField] private ElectricBoxBreaker connectedBreaker;

    [Header("Requirement & Target")]
    [SerializeField] private string requiredItemID = "BreakerLever";
    [SerializeField] private Transform snapPoint;
    [SerializeField] private string promptText = "Attach Lever";
    [SerializeField] private bool destroyObjectOnSnap = true;

    [Header("Narrative & Objectives")]
    [SerializeField] private ObjectiveSO requiredObjective;
    [SerializeField] private ObjectiveSO objectiveToActivateOnSnap;
    [SerializeField] private DialogueSO lockedDialogue;
    [SerializeField] private DialogueSO postSnapDialogue;

    [Header("Audio & Events")]
    [SerializeField] private AudioClip snapSFX;
    [SerializeField] private UnityEvent onLeverSnapped;

    private Collider triggerCollider;
    private bool isSnapped = false;

    public bool DestroyObjectOnSnap => destroyObjectOnSnap;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    public bool IsSnapZoneUnlocked()
    {
        if (requiredObjective == null) return true;

        if (ObjectiveManager.Instance != null)
        {
            return ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) == ObjectiveStatus.Active;
        }

        return false;
    }

    public void Interact()
    {
    }

    public bool CanSnap(ObjectGrabbable heldItem)
    {
        if (isSnapped || !IsSnapZoneUnlocked()) return false;
        if (heldItem == null) return false;

        return heldItem.ItemID == requiredItemID;
    }

    public string GetPromptMessage(ObjectGrabbable heldItem)
    {
        if (isSnapped || !IsSnapZoneUnlocked()) return string.Empty;
        if (heldItem == null) return string.Empty;

        if (CanSnap(heldItem))
        {
            return promptText;
        }

        return string.Empty;
    }

    public bool TrySnapItem(ObjectGrabbable heldItem, out Transform snapTarget)
    {
        snapTarget = snapPoint != null ? snapPoint : transform;

        if (!IsSnapZoneUnlocked())
        {
            TriggerLockedDialogue();
            return false;
        }

        if (!CanSnap(heldItem))
        {
            return false;
        }

        isSnapped = true;

        if (connectedBreaker != null)
        {
            connectedBreaker.AttachMissingLever();
        }

        if (snapSFX != null)
        {
            AudioSource.PlayClipAtPoint(snapSFX, transform.position);
        }

        onLeverSnapped?.Invoke();

        StartCoroutine(CompleteSequenceRoutine());

        return true;
    }

    private IEnumerator CompleteSequenceRoutine()
    {
        yield return new WaitForEndOfFrame();

        CompleteSnapSequence();
    }

    private void CompleteSnapSequence()
    {
        if (objectiveToActivateOnSnap != null && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ActivateObjective(objectiveToActivateOnSnap);
        }

        if (postSnapDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(postSnapDialogue);
        }

        CleanupSnapZone();
    }

    private void CleanupSnapZone()
    {
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        WorldSpaceInteractableUI[] childUIs = GetComponentsInChildren<WorldSpaceInteractableUI>(true);
        foreach (WorldSpaceInteractableUI ui in childUIs)
        {
            if (ui != null)
            {
                Destroy(ui.gameObject);
            }
        }

        enabled = false;
    }

    private void TriggerLockedDialogue()
    {
        if (lockedDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(lockedDialogue);
        }
    }
}