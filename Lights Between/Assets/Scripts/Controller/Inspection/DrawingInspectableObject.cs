using UnityEngine;

[RequireComponent(typeof(InspectableObject))]
public class DrawingInspectableObject : MonoBehaviour
{
    private static bool hasSeenFirstDrawing;

    [Header("First Drawing Dialogue (3 Lines)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] firstDrawingThoughts = new string[3];

    [Header("Second Drawing Dialogue (3 Lines)")]
    [TextArea(2, 4)]
    [SerializeField] private string[] secondDrawingThoughts = new string[3];

    [SerializeField] private InspectableObject inspectableObject;

    private void Awake()
    {
        if (inspectableObject == null)
        {
            inspectableObject = GetComponent<InspectableObject>();
        }

        if (inspectableObject == null)
        {
            Debug.Log("Inspectable Object not found");
        }
    }

    public void PrepareThoughts()
    {
        if (inspectableObject == null) return;

        string[] chosenThoughts = hasSeenFirstDrawing ? secondDrawingThoughts : firstDrawingThoughts;
        hasSeenFirstDrawing = true;

        inspectableObject.InternalThoughts = chosenThoughts;
    }

    public static void ResetDrawingState()
    {
        hasSeenFirstDrawing = false;
    }
}