using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueSO dialogueToTrigger;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    public void TriggerDialogue()
    {
        if (hasTriggered && triggerOnce) return;

        if (DialogueManager.Instance != null && dialogueToTrigger != null)
        {
            hasTriggered = true;
            DialogueManager.Instance.StartDialogue(dialogueToTrigger);
        }
    }
}