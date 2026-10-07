using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlashlightUIScript : MonoBehaviour
{
    [Header("References", order = 0)]
    [Space(5)]
    public HoldInteraction HoldInteraction;

    [Header("UI Containers & Components", order = 1)]
    [Space(5)]
    [SerializeField] private GameObject batteryUIPanel;
    [SerializeField] private CanvasGroup changeBatteryPromptCanvasGroup;
    [SerializeField] private GameObject batteryProgressObj;
    [SerializeField] private Image[] batteryIcons = new Image[3];

    [Header("Color Settings", order = 2)]
    [Space(5)]
    [SerializeField] private Color fullBatteryColor = Color.white;
    [SerializeField] private Color emptyBatteryColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Header("Battery Prompt Components", order = 3)]
    [Space(5)]
    [SerializeField] private TextMeshProUGUI changeBatteryPromptText;
    [SerializeField] private UITextFlasher promptTextFlasher;
    [SerializeField] private string replaceBatteryText = "Hold [X] to replace battery";
    [SerializeField] private string noBatteriesText = "No Batteries";

    [Header("Tutorial Prompt Components", order = 4)]
    [Space(5)]
    [SerializeField] private TextMeshProUGUI tutorialPromptText;
    [SerializeField] private CanvasGroup tutorialPromptCanvasGroup;

    public TextMeshProUGUI TutorialPromptText => tutorialPromptText;
    public CanvasGroup TutorialPromptCanvasGroup => tutorialPromptCanvasGroup;

    private void Awake()
    {
        SetUIVisibility(false);

        if (tutorialPromptCanvasGroup != null)
        {
            tutorialPromptCanvasGroup.alpha = 0f;
        }

        if (tutorialPromptText != null)
        {
            tutorialPromptText.text = string.Empty;
        }
    }

    private void Start()
    {
        SetBatteryPromptState(false, false);
    }

    public void SetUIVisibility(bool visible)
    {
        if (batteryUIPanel != null)
        {
            batteryUIPanel.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }

        if (!visible)
        {
            SetBatteryPromptState(false, false);
        }
    }

    public void SetBatteryPromptState(bool active, bool hasSpareBattery)
    {
        if (changeBatteryPromptCanvasGroup != null)
        {
            changeBatteryPromptCanvasGroup.alpha = active ? 1f : 0f;
            changeBatteryPromptCanvasGroup.interactable = active;
            changeBatteryPromptCanvasGroup.blocksRaycasts = active;
        }

        if (batteryProgressObj != null)
        {
            ToggleProgress(hasSpareBattery);
        }

        if (changeBatteryPromptText != null)
        {
            changeBatteryPromptText.text = hasSpareBattery ? replaceBatteryText : noBatteriesText;
        }
    }

    public void ToggleProgress(bool progress)
    {
        if (batteryProgressObj != null)
        {
            batteryProgressObj.SetActive(progress);
        }
    }

    public void TriggerEmptyFlash()
    {
        if (promptTextFlasher != null)
        {
            promptTextFlasher.PlayFlashEffect();
        }
    }

    public void UpdateBatteryUI(int currentBatteryCount, bool isCurrentBatteryDepleted)
    {
        if (batteryIcons == null || batteryIcons.Length == 0) return;

        int iconsToDisplay = Mathf.Min(currentBatteryCount, batteryIcons.Length);

        for (int i = 0; i < batteryIcons.Length; i++)
        {
            if (batteryIcons[i] == null) continue;

            if (i < iconsToDisplay)
            {
                batteryIcons[i].gameObject.SetActive(true);
                batteryIcons[i].color = (i == 0 && isCurrentBatteryDepleted) ? emptyBatteryColor : fullBatteryColor;
            }
            else
            {
                batteryIcons[i].gameObject.SetActive(false);
            }
        }
    }
}