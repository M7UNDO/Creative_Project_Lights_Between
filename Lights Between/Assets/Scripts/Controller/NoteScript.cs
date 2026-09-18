using UnityEngine;

public class NoteScript : MonoBehaviour, IInteractable
{
    [SerializeField] private NoteData noteData;

    public string promptMessage { get; private set; } = "Read Note";

    public bool CanInteract()
    {
        return noteData != null;
    }

    public void Interact()
    {
        if (noteData == null) return;

        if (NoteUIController.Instance != null)
        {
            NoteUIController.Instance.OpenNote(noteData);
        }
    }
}