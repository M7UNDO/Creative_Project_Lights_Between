using UnityEngine;
using UnityEngine.UI;

public class InputPrompt : MonoBehaviour
{
    [Header("Target Image")]
    [SerializeField] private Image promptImage;

    [Header("Sprites")]
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite xboxSprite;
    [SerializeField] private Sprite playStationSprite;

    public void Awake()
    {
        if (promptImage == null)
        {
            promptImage = GetComponent<Image>();
        }


    }

    private void Start()
    {

        UpdateSprite(InputDeviceDetector.Instance.CurrentDevice);
        InputDeviceDetector.Instance.OnDeviceChanged += UpdateSprite;
    }

    private void OnDestroy()
    {
        if (InputDeviceDetector.Instance != null)
            InputDeviceDetector.Instance.OnDeviceChanged -= UpdateSprite;
    }

    private void UpdateSprite(InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceType.KeyboardMouse:
                promptImage.sprite = keyboardSprite;
                break;

            case InputDeviceType.Xbox:
                promptImage.sprite = xboxSprite;
                break;

            case InputDeviceType.PlayStation:
                promptImage.sprite = playStationSprite;
                break;
        }
    }
}
