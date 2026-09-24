using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueSO dialogueToTrigger;
    [SerializeField] private bool triggerOnce = true;

    [Header("Optional Event Hooks")]
    [SerializeField] private GameEventSO onDialogueTriggeredEvent;
    [SerializeField] private UnityEvent onTriggered;

    [SerializeField] private bool delaySpeech = false;
    [SerializeField] private float delayTime;


    private bool hasTriggered;

    public void TriggerDialogue()
    {
        if (hasTriggered && triggerOnce) return;

        if (delaySpeech)
        {
            StartCoroutine(DelaySpeechRoutine());
        }
        else
        {
            ExecuteDialogue();
        } 
    }

    private void ExecuteDialogue()
    {
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

    private IEnumerator DelaySpeechRoutine()
    {
        if (triggerOnce)
        {
            hasTriggered = true;
        }

        yield return new WaitForSeconds(delayTime);
        ExecuteDialogue();
    }


}