using UnityEngine;

public enum ToolType
{
    Flashlight,
    ParaffinLamp,
    Lighter,
    GeneratorFuel,
    Keys,
    None
}

[CreateAssetMenu(fileName = "New Tool", menuName = "Inventory/Tool")]
public class ToolClass : ScriptableObject
{
    [Header("Tool Configuration")]
    [Space(5)]
    public string toolName;
    public ToolType toolType;
    public GameObject toolPrefab;
    public Sprite sprite;

    [Header("Stacking Settings")]
    [Space(5)]
    public bool isStackable = true;
    public int stackSize = 64;

    [Header("Hold Transform Offsets")]
    [Space(5)]
    public Vector3 holdPointPositionOffset;
    public Vector3 holdPointRotationOffset;

    [Header("Power Settings")]
    [Space(5)]
    public bool usesPower;
    public float maxPower;

    [Header("Radial Menu Placement")]
    [Space(5)]
    [Range(0, 7)]
    public int fixedRadialSlotIndex;
}