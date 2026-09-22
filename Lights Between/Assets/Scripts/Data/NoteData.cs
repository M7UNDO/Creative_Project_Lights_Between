using UnityEngine;

[CreateAssetMenu(fileName = "NewNoteData", menuName = "Notes/Note Data")]
public class NoteData : ScriptableObject
{
    [SerializeField, TextArea(10, 20)] private string[] pages = new string[1];
    [SerializeField] private bool triggersObjective = false;

    public string[] Pages => pages;
    public bool TriggersObjective => triggersObjective;

    public string GetPageContent(int index)
    {
        if (pages == null || pages.Length == 0) return string.Empty;
        int clampedIndex = Mathf.Clamp(index, 0, pages.Length - 1);
        return pages[clampedIndex];
    }

    public int TotalPages => pages != null && pages.Length > 0 ? pages.Length : 1;
}