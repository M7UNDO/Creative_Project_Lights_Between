using UnityEngine;

public class StoryEventDialogueBridge : MonoBehaviour
{
    [SerializeField] private GameEventSO storyEventChannel;
    [SerializeField] private DialogueSO dialogueToStart;

    private void OnEnable()
    {
        if (storyEventChannel != null)
        {
            storyEventChannel.RegisterListener(HandleStoryEvent);
        }
    }

    private void OnDisable()
    {
        if (storyEventChannel != null)
        {
            storyEventChannel.UnregisterListener(HandleStoryEvent);
        }
    }

    private void HandleStoryEvent()
    {
        if (DialogueManager.Instance != null && dialogueToStart != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueToStart);
        }
    }
}