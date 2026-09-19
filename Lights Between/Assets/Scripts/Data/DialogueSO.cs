using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue_", menuName = "The Lights Between/Dialogue/Dialogue Sequence")]
public class DialogueSO : ScriptableObject
{
    [Header("Lines")]
    [TextArea(2, 4)]
    [SerializeField] private string[] lines;

    [Header("Narrative Consequences")]
    [SerializeField] private GameEventSO onDialogueCompletedEvent;
    [SerializeField] private ObjectiveSO objectiveToActivateOnComplete;

    public string[] Lines => lines;
    public GameEventSO OnDialogueCompletedEvent => onDialogueCompletedEvent;
    public ObjectiveSO ObjectiveToActivateOnComplete => objectiveToActivateOnComplete;
}