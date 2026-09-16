using UnityEngine;

[CreateAssetMenu(fileName = "Obj_", menuName = "The Lights Between/Objectives/Objective")]
public class ObjectiveSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string title;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private string targetInteractionID;

    public string ID => id;
    public string Title => title;
    public string Description => description;
    public string TargetInteractionID => targetInteractionID;
}