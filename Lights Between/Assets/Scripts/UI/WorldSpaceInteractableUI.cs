using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldSpaceInteractableUI : MonoBehaviour
{
    public static HashSet<WorldSpaceInteractableUI> AllInteractableUIs = new HashSet<WorldSpaceInteractableUI>();

    public enum UIState
    {
        Hidden,
        Skeleton,
        FullPrompt
    }

    [SerializeField] private Canvas worldCanvas;

    [Header("Primary Prompt (Pickup / Standard)")]
    [SerializeField] private TextMeshProUGUI promptTextContainer;
    [SerializeField] private GameObject inputIconObject;
    [SerializeField] private GameObject skeletonIconObject;

    [Header("Secondary Prompt (Ignite / Optional)")]
    [SerializeField] private TextMeshProUGUI ignitePromptTextContainer;
    [SerializeField] private GameObject igniteInputIconObject;
    [SerializeField] private GameObject igniteSkeletonIconObject;

    [Header("Detection Ranges")]
    [SerializeField] private float proximityDistance = 8f;

    [Header("Icon Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(200f / 255f, 200f / 255f, 200f / 255f, 128f / 255f);

    private Transform _mainCameraTransform;
    public Image _inputIconImage;
    private Image _skeletonIconImage;

    public Image _igniteInputIconImage;
    private Image _igniteSkeletonIconImage;

    private UIState _currentState = UIState.Hidden;
    private bool _isDisabledBySystem;

    public float ProximityDistance => proximityDistance;
    public UIState CurrentState => _currentState;
    public bool IsDisabledBySystem => _isDisabledBySystem;

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

        if (skeletonIconObject != null)
        {
            _skeletonIconImage = skeletonIconObject.GetComponent<Image>();
        }

        if (igniteInputIconObject != null)
        {
            _igniteInputIconImage = igniteInputIconObject.GetComponent<Image>();
        }

        if (igniteSkeletonIconObject != null)
        {
            _igniteSkeletonIconImage = igniteSkeletonIconObject.GetComponent<Image>();
        }

        SetUIState(UIState.Hidden);
    }

    private void OnEnable()
    {
        AllInteractableUIs.Add(this);
    }

    private void OnDisable()
    {
        AllInteractableUIs.Remove(this);
    }

    private void LateUpdate()
    {
        if (worldCanvas != null && worldCanvas.enabled && _mainCameraTransform != null)
        {
            transform.LookAt(transform.position + _mainCameraTransform.rotation * Vector3.forward, _mainCameraTransform.rotation * Vector3.up);
        }
    }

    public void SetSystemDisabled(bool disabled)
    {
        _isDisabledBySystem = disabled;
        if (_isDisabledBySystem)
        {
            SetUIState(UIState.Hidden);
        }
    }

    public void SetUIState(UIState newState, string message = "", bool showInputIcon = true, bool isInteractable = true)
    {
        if (_isDisabledBySystem)
        {
            if (worldCanvas != null) worldCanvas.enabled = false;
            _currentState = UIState.Hidden;
            return;
        }

        _currentState = newState;

        switch (newState)
        {
            case UIState.Hidden:
                if (worldCanvas != null) worldCanvas.enabled = false;
                if (ignitePromptTextContainer != null) ignitePromptTextContainer.gameObject.SetActive(false);
                if (igniteInputIconObject != null) igniteInputIconObject.SetActive(false);
                if (igniteSkeletonIconObject != null) igniteSkeletonIconObject.SetActive(false);
                break;

            case UIState.Skeleton:
                if (worldCanvas != null) worldCanvas.enabled = true;
                if (promptTextContainer != null) promptTextContainer.gameObject.SetActive(false);
                if (ignitePromptTextContainer != null) ignitePromptTextContainer.gameObject.SetActive(false);
                if (inputIconObject != null) inputIconObject.SetActive(false);
                if (igniteInputIconObject != null) igniteInputIconObject.SetActive(false);
                if (igniteSkeletonIconObject != null) igniteSkeletonIconObject.SetActive(false);

                if (skeletonIconObject != null)
                {
                    skeletonIconObject.SetActive(true);
                    if (_skeletonIconImage != null)
                    {
                        _skeletonIconImage.color = isInteractable ? activeColor : disabledColor;
                    }
                }
                break;

            case UIState.FullPrompt:
                if (worldCanvas != null) worldCanvas.enabled = true;
                if (promptTextContainer != null)
                {
                    promptTextContainer.gameObject.SetActive(true);
                    promptTextContainer.text = message;
                }
                if (ignitePromptTextContainer != null) ignitePromptTextContainer.gameObject.SetActive(false);
                if (igniteInputIconObject != null) igniteInputIconObject.SetActive(false);
                if (igniteSkeletonIconObject != null) igniteSkeletonIconObject.SetActive(false);

                if (skeletonIconObject != null) skeletonIconObject.SetActive(false);
                if (inputIconObject != null)
                {
                    inputIconObject.SetActive(showInputIcon);
                    if (_inputIconImage != null)
                    {
                        _inputIconImage.color = isInteractable ? activeColor : disabledColor;
                    }
                }
                break;
        }
    }

    public void UpdateDualPrompts(string pickupMessage, bool showPickup, string igniteMessage, bool showIgnite, bool showInputIcon = true, bool isInteractable = true)
    {
        if (_isDisabledBySystem)
        {
            if (worldCanvas != null) worldCanvas.enabled = false;
            _currentState = UIState.Hidden;
            return;
        }

        _currentState = UIState.FullPrompt;

        if (worldCanvas != null) worldCanvas.enabled = showPickup || showIgnite;

        if (promptTextContainer != null)
        {
            promptTextContainer.gameObject.SetActive(showPickup);
            promptTextContainer.text = pickupMessage;
        }

        if (inputIconObject != null)
        {
            inputIconObject.SetActive(showInputIcon && showPickup);
            if (_inputIconImage != null)
            {
                _inputIconImage.color = isInteractable ? activeColor : disabledColor;
            }
        }

        if (ignitePromptTextContainer != null)
        {
            ignitePromptTextContainer.gameObject.SetActive(showIgnite);
            ignitePromptTextContainer.text = igniteMessage;
        }

        if (igniteInputIconObject != null)
        {
            igniteInputIconObject.SetActive(showInputIcon && showIgnite);
            if (_igniteInputIconImage != null)
            {
                _igniteInputIconImage.color = isInteractable ? activeColor : disabledColor;
            }
        }

        if (skeletonIconObject != null) skeletonIconObject.SetActive(false);
        if (igniteSkeletonIconObject != null) igniteSkeletonIconObject.SetActive(false);
    }

    public void ShowPrompt(string message, bool showInputIcon = true, bool isInteractable = true)
    {
        SetUIState(UIState.FullPrompt, message, showInputIcon, isInteractable);
    }

    public void ShowSkeleton(bool isInteractable = true)
    {
        SetUIState(UIState.Skeleton, "", false, isInteractable);
    }

    public void HidePrompt()
    {
        SetUIState(UIState.Hidden);
    }
}