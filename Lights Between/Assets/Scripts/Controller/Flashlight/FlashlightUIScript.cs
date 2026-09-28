using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlashlightUIScript : MonoBehaviour
{
    [Header("References")]
    public HoldInteraction HoldInteraction;

    [Header("UI Containers & Components")]
    [SerializeField] private GameObject batteryUIPanel;
    [SerializeField] private CanvasGroup changeBatteryPromptCanvasGroup;
    [SerializeField] private GameObject batteryProgressObj;
    [SerializeField] private Image[] batteryIcons = new Image[3];

    [Header("Color Settings")]
    [SerializeField] private Color fullBatteryColor = Color.white;
    [SerializeField] private Color emptyBatteryColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Header("Battery Prompt Components")]
    [SerializeField] private TextMeshProUGUI changeBatteryPromptText;
    [SerializeField] private UITextFlasher promptTextFlasher;
    [SerializeField] private string replaceBatteryText = "Hold [X] to replace battery";
    [SerializeField] private string noBatteriesText = "No Batteries";

    [Header("Tutorial Prompt Components")]
    [SerializeField] private TextMeshProUGUI tutorialPromptText;
    [SerializeField] private CanvasGroup tutorialPromptCanvasGroup;

    public TextMeshProUGUI TutorialPromptText => tutorialPromptText;
    public CanvasGroup TutorialPromptCanvasGroup => tutorialPromptCanvasGroup;

    private void Awake()
    {
        SetUIVisibility(false);
        SetBatteryPromptState(false, false);

        if (tutorialPromptCanvasGroup != null)
        {
            tutorialPromptCanvasGroup.alpha = 0f;
        }

        if (tutorialPromptText != null)
        {
            tutorialPromptText.text = "";
        }
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
        batteryProgressObj.SetActive(progress); 
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

        for (int i = 0; i < batteryIcons.Length; i++)
        {
            if (batteryIcons[i] == null) continue;

            if (i < currentBatteryCount)
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