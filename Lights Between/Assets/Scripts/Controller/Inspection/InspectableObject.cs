using UnityEngine;

public class InspectableObject : MonoBehaviour, IInteractable
{
    [Header("Camera Focal Point")]
    [Tooltip("Target transform the camera will focus on during inspection.")]
    [SerializeField] private Transform inspectCameraTransform;

    [Header("Drawing / Image UI Settings")]
    [SerializeField] private bool isDrawingUI;
    [SerializeField] private bool isPortrait;
    [SerializeField] private bool isLandscape;
    [SerializeField] private Sprite drawingSprite;

    [Header("Dialogue / Thoughts")]
    [TextArea(2, 4)]
    [SerializeField] private string[] internalThoughts;

    private bool hasTriggeredFirstInspect;

    public Transform InspectCameraTransform => inspectCameraTransform != null ? inspectCameraTransform : transform;
    public bool IsPortrait => isPortrait;
    public bool IsLandscape => isLandscape;

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

        if (isDrawingUI && drawingSprite != null)
        {
            if (DrawingUIController.Instance != null)
            {
                InspectableObject objectToInspectOnClose = null;

                if (!hasTriggeredFirstInspect)
                {
                    hasTriggeredFirstInspect = true;
                    objectToInspectOnClose = this;
                }

                DrawingUIController.Instance.OpenDrawing(drawingSprite, this, objectToInspectOnClose);
                return;
            }
        }

        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.StartInspection(this);
        }
    }
}