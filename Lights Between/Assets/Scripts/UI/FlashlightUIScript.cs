using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlashlightUIScript : MonoBehaviour
{
    [Header("UI Containers & Components")]
    [SerializeField] private GameObject batteryUIPanel;
    [SerializeField] private Image[] batteryIcons = new Image[3];

    [Header("Color Settings")]
    [SerializeField] private Color fullBatteryColor = Color.white;
    [SerializeField] private Color emptyBatteryColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Header("Tutorial Prompt Components")]
    [SerializeField] private TextMeshProUGUI tutorialPromptText;
    [SerializeField] private CanvasGroup tutorialPromptCanvasGroup;

    private Coroutine promptCoroutine;

    private void Awake()
    {
        SetUIVisibility(false);

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

    public void ShowTutorialPrompt(string promptText, float duration, float fadeDuration)
    {
        if (tutorialPromptText == null || tutorialPromptCanvasGroup == null) return;

        if (promptCoroutine != null)
        {
            StopCoroutine(promptCoroutine);
        }

        promptCoroutine = StartCoroutine(DisplayPromptRoutine(promptText, duration, fadeDuration));
    }

    public void UpdatePromptText(string promptText)
    {
        if (tutorialPromptText != null)
        {
            tutorialPromptText.text = promptText;
        }
    }

    private IEnumerator DisplayPromptRoutine(string message, float duration, float fadeDuration)
    {
        tutorialPromptText.text = message;

        float timer = 0f;
        float startAlpha = tutorialPromptCanvasGroup.alpha;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            tutorialPromptCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, timer / fadeDuration);
            yield return null;
        }

        tutorialPromptCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            tutorialPromptCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        tutorialPromptCanvasGroup.alpha = 0f;
        tutorialPromptText.text = "";
        promptCoroutine = null;
    }
}