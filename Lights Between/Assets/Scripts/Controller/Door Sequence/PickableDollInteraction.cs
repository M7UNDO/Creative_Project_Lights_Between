using UnityEngine;

public class PickableDollInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueSO initialDollDialogue;
    [SerializeField] private ObjectGrabbable grabbableComponent;

    private bool hasSpoken;

    private void Awake()
    {
        if (grabbableComponent == null)
        {
            grabbableComponent = GetComponent<ObjectGrabbable>();
        }

        if (grabbableComponent != null)
        {
            grabbableComponent.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }
    }

    public void Interact()
    {
        if (hasSpoken) return;

        hasSpoken = true;

        if (DialogueManager.Instance != null && initialDollDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(initialDollDialogue);
        }
        else
        {
            CompleteInteraction();
        }
    }

    private void HandleDialogueEnded()
    {
        if (hasSpoken)
        {
            CompleteInteraction();
        }
    }

    private void CompleteInteraction()
    {
        if (grabbableComponent != null)
        {
            grabbableComponent.enabled = true;
        }

        Destroy(this);
    }
}