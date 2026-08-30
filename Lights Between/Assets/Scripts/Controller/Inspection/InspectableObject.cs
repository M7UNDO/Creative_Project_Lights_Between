using UnityEngine;

public class InspectableObject : MonoBehaviour, IInteractable
{
    [Header("Prompt Settings")]
    [SerializeField] private string promptMessage = "Examine";

    [Header("Camera Focal Point")]
    [Tooltip("Target transform the camera will focus on during inspection.")]
    [SerializeField] private Transform inspectCameraTransform;

    [Header("Dialogue / Thoughts")]
    [TextArea(2, 4)]
    [SerializeField] private string[] internalThoughts;

    [Header("UI Reference")]
    [SerializeField] private WorldSpaceInteractableUI worldUI;

    public string PromptMessage => promptMessage;
    public Transform InspectCameraTransform => inspectCameraTransform != null ? inspectCameraTransform : transform;
    public string[] InternalThoughts => internalThoughts;
    public WorldSpaceInteractableUI WorldUI => worldUI;

    private void Awake()
    {
        if (worldUI == null)
        {
            worldUI = GetComponentInChildren<WorldSpaceInteractableUI>();
        }
    }

    public void Interact()
    {
        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.StartInspection(this);
        }
    }
}