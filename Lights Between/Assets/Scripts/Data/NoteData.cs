using UnityEngine;

[CreateAssetMenu(fileName = "NewNoteData", menuName = "Notes/Note Data")]
public class NoteData : ScriptableObject
{
    [SerializeField, TextArea(10, 20)] private string noteContent;

    public string NoteContent => noteContent;
}