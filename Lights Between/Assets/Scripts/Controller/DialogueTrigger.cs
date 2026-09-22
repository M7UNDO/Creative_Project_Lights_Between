using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueSO dialogueToTrigger;
    [SerializeField] private bool triggerOnce = true;

    [Header("Optional Event Hooks")]
    [SerializeField] private GameEventSO onDialogueTriggeredEvent;
    [SerializeField] private UnityEvent onTriggered;

    private bool hasTriggered;

    public void TriggerDialogue()
    {
        if (hasTriggered && triggerOnce) return;

        if (DialogueManager.Instance != null && dialogueToTrigger != null)
        {
            hasTriggered = true;

            if (onDialogueTriggeredEvent != null)
            {
                onDialogueTriggeredEvent.Raise();
            }

            onTriggered?.Invoke();

            DialogueManager.Instance.StartDialogue(dialogueToTrigger);
        }
    }
}