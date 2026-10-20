using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SnapZone : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public struct SnapRequirement
    {
        public string itemID;
        public Transform snapPoint;
        public GameObject visualToEnable;
        public GameObject prefabToInstantiate;
        [HideInInspector] public bool isSnapped;
    }

    [Header("Narrative/Objective Lock")]
    [SerializeField] private ObjectiveSO requiredObjective;
    [SerializeField] private DialogueSO lockedDialogue;

    [Header("Requirements & Mapping")]
    [SerializeField] private List<SnapRequirement> requirements = new List<SnapRequirement>();
    [SerializeField] private string promptText = "Place Luggage";

    [Header("Snap Behavior")]
    [SerializeField] private bool destroyObjectOnSnap = true;
    public bool DestroyObjectOnSnap => destroyObjectOnSnap;

    [Header("Objective Progression")]
    [SerializeField] private ObjectiveSO performRitualObjective;

    [Header("Completion Cleanup")]
    [SerializeField] private bool destroySnapZoneOnComplete = false;

    [Header("Events & Audio")]
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string interactionEventID = "LuggagePlaced";
    [SerializeField] private AudioClip snapSFX;

    [Header("Unity Event Hooks")]
    [SerializeField] private UnityEvent onSingleItemSnapped;
    [SerializeField] private UnityEvent onSnapComplete;

    [Header("Invalid Item Handling")]
    [SerializeField] private List<string> invalidItemIDs = new List<string> { "UncookedPizza" };
    [SerializeField] private DialogueSO invalidItemDialogue;

    [Header("Dialogue Trigger")]
    [SerializeField] private DialogueSO postSnapDialogue;

    private int currentSnappedCount = 0;
    private Collider triggerCollider;

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

    public bool TrySnapItem(ObjectGrabbable heldItem, out Transform snapTarget)
    {
        snapTarget = null;

        if (!IsSnapZoneUnlocked())
        {
            TriggerLockedDialogue();
            return false;
        }

        if (IsComplete() || heldItem == null) return false;

        if (IsInvalidItem(heldItem.ItemID))
        {
            TriggerInvalidDialogue();
            return false;
        }

        for (int i = 0; i < requirements.Count; i++)
        {
            SnapRequirement req = requirements[i];

            if (!req.isSnapped && req.itemID == heldItem.ItemID)
            {
                snapTarget = req.snapPoint != null ? req.snapPoint : transform;

                if (req.prefabToInstantiate != null)
                {
                    Instantiate(req.prefabToInstantiate, snapTarget.position, snapTarget.rotation, snapTarget);
                }
                else if (req.visualToEnable != null)
                {
                    req.visualToEnable.SetActive(true);
                }

                req.isSnapped = true;
                requirements[i] = req;

                currentSnappedCount++;

                if (snapSFX != null)
                {
                    AudioSource.PlayClipAtPoint(snapSFX, transform.position);
                }

                onSingleItemSnapped?.Invoke();

                if (currentSnappedCount >= requirements.Count)
                {
                    StartCoroutine(CompleteSequenceRoutine());
                }

                return true;
            }
        }

        return false;
    }

    private IEnumerator CompleteSequenceRoutine()
    {
        yield return new WaitForEndOfFrame();

        CompleteSnapSequence();
    }

    private void CompleteSnapSequence()
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

    public bool CanSnap(ObjectGrabbable heldItem)
    {
        if (!IsSnapZoneUnlocked()) return false;
        if (IsComplete() || heldItem == null) return false;

        if (IsInvalidItem(heldItem.ItemID)) return true;

        foreach (SnapRequirement req in requirements)
        {
            if (!req.isSnapped && req.itemID == heldItem.ItemID)
            {
                return true;
            }
        }

        return false;
    }

    public string GetPromptMessage(ObjectGrabbable heldItem)
    {
        if (!IsSnapZoneUnlocked()) return string.Empty;
        if (IsComplete() || heldItem == null) return string.Empty;

        if (IsInvalidItem(heldItem.ItemID))
        {
            return "Place Item";
        }

        if (CanSnap(heldItem))
        {
            return $"{promptText} ({currentSnappedCount}/{requirements.Count})";
        }

        return string.Empty;
    }

    private bool IsInvalidItem(string itemID)
    {
        return invalidItemIDs.Contains(itemID);
    }

    private bool IsComplete()
    {
        return currentSnappedCount >= requirements.Count;
    }

    private void TriggerInvalidDialogue()
    {
        if (DialogueManager.Instance != null && invalidItemDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(invalidItemDialogue);
        }
    }

    private void TriggerLockedDialogue()
    {
        if (DialogueManager.Instance != null && lockedDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(lockedDialogue);
        }
    }
}