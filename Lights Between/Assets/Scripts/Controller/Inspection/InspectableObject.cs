using UnityEngine;

public class InspectableObject : MonoBehaviour, IInteractable
{
    [Header("Camera Focal Point")]
    [Tooltip("Target transform the camera will focus on during inspection.")]
    [SerializeField] private Transform inspectCameraTransform;

    [Header("Dialogue / Thoughts")]
    [TextArea(2, 4)]
    [SerializeField] private string[] internalThoughts;

    public Transform InspectCameraTransform => inspectCameraTransform != null ? inspectCameraTransform : transform;

    public string[] InternalThoughts
    {
        get => internalThoughts;
        set => internalThoughts = value;
    }

    public void Interact()
    {
        if (TryGetComponent<DrawingInspectableObject>(out var drawingHelper))
        {
            drawingHelper.PrepareThoughts();
        }

        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.StartInspection(this);
        }
    }
}