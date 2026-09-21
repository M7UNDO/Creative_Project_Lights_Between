using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue_", menuName = "The Lights Between/Dialogue/Dialogue Sequence")]
public class DialogueSO : ScriptableObject
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        public Color textColor;
        [TextArea(2, 4)] public string text;
    }

    [Header("Lines")]
    [SerializeField] private DialogueLine[] formattedLines;
    [TextArea(2, 4)]
    [SerializeField] private string[] lines;

    [Header("Narrative Consequences")]
    [SerializeField] private GameEventSO onDialogueCompletedEvent;
    [SerializeField] private ObjectiveSO objectiveToActivateOnComplete;

    public DialogueLine[] FormattedLines => formattedLines;
    public string[] Lines => lines;
    public GameEventSO OnDialogueCompletedEvent => onDialogueCompletedEvent;
    public ObjectiveSO ObjectiveToActivateOnComplete => objectiveToActivateOnComplete;
}