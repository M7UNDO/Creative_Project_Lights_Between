using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class InputTextPrompt : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;

    [Header("Format Settings")]
    [TextArea]
    [SerializeField] private string formatTemplate = "Press {0} to Start";

    [Header("Keyboard Text")]
    [SerializeField] private string keyboardInputText = "[Space]";

    [Header("Controller Sprite Names")]
    [SerializeField] private string xboxSpriteName = "Xbox_A";
    [SerializeField] private string playstationSpriteName = "PS_Cross";

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (InputDeviceDetector.Instance != null)
        {
            UpdatePromptText(InputDeviceDetector.Instance.CurrentDevice);
            InputDeviceDetector.Instance.OnDeviceChanged += UpdatePromptText;
        }
    }

    private void OnDestroy()
    {
        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= UpdatePromptText;
    }

    private void UpdatePromptText(InputDeviceType deviceType)
    {
        string promptValue = GetPromptValueForDevice(deviceType);
        textComponent.text = string.Format(formatTemplate, promptValue);
    }

    private string GetPromptValueForDevice(InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceType.KeyboardMouse:
                return keyboardInputText;

            case InputDeviceType.Xbox:
                return string.IsNullOrEmpty(xboxSpriteName) ? string.Empty : $"<sprite name=\"{xboxSpriteName}\">";

            case InputDeviceType.PlayStation:
                return string.IsNullOrEmpty(playstationSpriteName) ? string.Empty : $"<sprite name=\"{playstationSpriteName}\">";

            default:
                return string.Empty;
        }
    }
}