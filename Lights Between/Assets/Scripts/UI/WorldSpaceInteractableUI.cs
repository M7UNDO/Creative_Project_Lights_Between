using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldSpaceInteractableUI : MonoBehaviour
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private TextMeshProUGUI promptTextContainer;
    [SerializeField] private GameObject inputIconObject;

    [Header("Icon Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(200f / 255f, 200f / 255f, 200f / 255f, 128f / 255f);

    private Transform _mainCameraTransform;
    private Image _inputIconImage;

    private void Awake()
    {
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }

        if (inputIconObject != null)
        {
            _inputIconImage = inputIconObject.GetComponent<Image>();
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

    public void ShowPrompt(string message, bool showInputIcon = true, bool isInteractable = true)
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

            if (_inputIconImage != null)
            {
                _inputIconImage.color = isInteractable ? activeColor : disabledColor;
            }
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