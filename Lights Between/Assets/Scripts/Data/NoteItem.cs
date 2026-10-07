using UnityEngine;

[CreateAssetMenu(fileName = "New Note Item", menuName = "Inventory/Items/Note")]
public class NoteItem : ItemClass
{
    [Header("Note Settings", order = 0)]
    [Space(5)]
    [TextArea(5, 10)] public string NoteContent;

    private void OnEnable()
    {
        UsePromptText = "Read";
    }

    public override bool CanUse(GameObject player)
    {
        return true;
    }

    public override void Use(GameObject player)
    {
        Debug.Log($"Reading note: {NoteContent}");
    }
}