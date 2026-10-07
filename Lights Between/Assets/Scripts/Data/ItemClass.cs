using UnityEngine;

public abstract class ItemClass : ScriptableObject
{
    [Header("Item Configuration", order = 0)]
    [Space(5)]
    public string ItemName;
    public Sprite ItemIcon;
    [TextArea(2, 5)] public string ItemDescription;
    public bool IsStackable = true;
    public int StackSize = 64;
    public string UsePromptText = "Use";

    public abstract bool CanUse(GameObject player);
    public abstract void Use(GameObject player);
}