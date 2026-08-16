using UnityEngine;
using TMPro;

public class WorldSpaceInteractableUI : MonoBehaviour
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private TextMeshProUGUI promptTextContainer;
    [SerializeField] private GameObject inputIconObject;

    private Transform _mainCameraTransform;

    private void Awake()
    {
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }

        HidePrompt();
    }

    private void LateUpdate()
    {
        if (worldCanvas != null && worldCanvas.enabled && _mainCameraTransform != null)
        {
            transform.LookAt(transform.position + _mainCameraTransform.rotation * Vector3.forward, _mainCameraTransform.rotation * Vector3.up);
        }
    }

    public void ShowPrompt(string message, bool showInputIcon = true)
    {
        if (worldCanvas != null)
        {
            worldCanvas.enabled = true;
        }

        if (promptTextContainer != null)
        {
            promptTextContainer.text = message;
        }

        if (inputIconObject != null)
        {
            inputIconObject.SetActive(showInputIcon);
        }
    }

    public void HidePrompt()
    {
        if (worldCanvas != null)
        {
            worldCanvas.enabled = false;
        }
    }
}