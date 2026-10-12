using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class InputTextPrompt : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;

    [Header("Format Settings")]
    [TextArea]
    [SerializeField] private string formatTemplate = "Press {0} to Start";

    [Header("Keyboard Settings")]
    [SerializeField] private bool useKeyboardSprites = false;
    [SerializeField] private string keyboardInputText = "[A] [D]";
    [SerializeField] private string keyboardPrimarySpriteName = "Keyboard_A";
    [SerializeField] private string keyboardSecondarySpriteName = "Keyboard_D";

    [Header("Xbox Sprite Names")]
    [SerializeField] private string xboxPrimarySpriteName = "Xbox_DPad_Left";
    [SerializeField] private string xboxSecondarySpriteName = "Xbox_DPad_Right";

    [Header("PlayStation Sprite Names")]
    [SerializeField] private string playstationPrimarySpriteName = "PS_DPad_Left";
    [SerializeField] private string playstationSecondarySpriteName = "PS_DPad_Right";

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
                return useKeyboardSprites
                    ? FormatDualSprites(keyboardPrimarySpriteName, keyboardSecondarySpriteName)
                    : keyboardInputText;

            case InputDeviceType.Xbox:
                return FormatDualSprites(xboxPrimarySpriteName, xboxSecondarySpriteName);

            case InputDeviceType.PlayStation:
                return FormatDualSprites(playstationPrimarySpriteName, playstationSecondarySpriteName);

            default:
                return string.Empty;
        }
    }

    private string FormatDualSprites(string primarySprite, string secondarySprite)
    {
        string result = string.Empty;

        if (!string.IsNullOrEmpty(primarySprite))
        {
            result += $"<sprite name=\"{primarySprite}\">";
        }

        if (!string.IsNullOrEmpty(secondarySprite))
        {
            if (!string.IsNullOrEmpty(result)) result += " ";
            result += $"<sprite name=\"{secondarySprite}\">";
        }

        return result;
    }
}