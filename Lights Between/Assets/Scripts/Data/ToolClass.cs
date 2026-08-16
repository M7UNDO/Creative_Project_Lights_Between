using UnityEngine;

public enum ToolType
{
    Flashlight,
    ParaffinLamp,
    GeneratorFuel,
    Keys,
    None
}

[CreateAssetMenu(fileName = "New Tool", menuName = "Inventory/Tool")]
public class ToolClass : ScriptableObject
{
    public string toolName;
    public ToolType toolType;
    public GameObject toolPrefab;
    public Sprite sprite;

    [Header("Hold Transform Offsets")]
    public Vector3 holdPointPositionOffset;
    public Vector3 holdPointRotationOffset;

    [Header("Power Settings")]
    public bool usesPower;
    public float maxPower;

    [Header("Radial Menu Placement")]
    [Range(0, 7)]
    public int fixedRadialSlotIndex;
}