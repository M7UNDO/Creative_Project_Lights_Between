using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CreditsManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private CreditData creditData;

    [Header("UI Containers")]
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private RectTransform viewportRect;

    [Header("UI Prefabs")]
    [SerializeField] private TextMeshProUGUI titlePrefab;
    [SerializeField] private TextMeshProUGUI headerPrefab;
    [SerializeField] private TextMeshProUGUI entryPrefab;

    [Header("UI Prompts")]
    [SerializeField] private GameObject fastForwardPrompt;
    [SerializeField] private GameObject backPrompt;

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float fastForwardMultiplier = 3f;
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float endDelay = 2f;

    [Header("Input")]
    [SerializeField] private InputActionReference fastForwardInput;

    private float currentSpeed;
    private bool isScrolling = false;
    private float totalContentHeight;
    private Coroutine creditSequenceCoroutine;

    private void OnEnable()
    {
        if (fastForwardInput != null && fastForwardInput.action != null)
        {
            fastForwardInput.action.Enable();
        }

        ResetCreditsSequence();
    }

    private void OnDisable()
    {
        if (fastForwardInput != null && fastForwardInput.action != null)
        {
            fastForwardInput.action.Disable();
        }

        if (creditSequenceCoroutine != null)
        {
            StopCoroutine(creditSequenceCoroutine);
            creditSequenceCoroutine = null;
        }

        isScrolling = false;
    }

    public void ResetCreditsSequence()
    {
        if (creditSequenceCoroutine != null)
        {
            StopCoroutine(creditSequenceCoroutine);
        }

        isScrolling = false;

        if (fastForwardPrompt != null) fastForwardPrompt.SetActive(false);
        if (backPrompt != null) backPrompt.SetActive(false);

        BuildCreditsUI();
        creditSequenceCoroutine = StartCoroutine(StartCreditSequence());
    }

    private void Update()
    {
        if (!isScrolling) return;

        bool isFastForwarding = fastForwardInput != null && fastForwardInput.action != null && fastForwardInput.action.IsPressed();

        if (isFastForwarding)
        {
            currentSpeed = scrollSpeed * fastForwardMultiplier;
        }
        else
        {
            currentSpeed = scrollSpeed;
        }

        contentContainer.anchoredPosition += Vector2.up * (currentSpeed * Time.deltaTime);

        float finishPosition = totalContentHeight + viewportRect.rect.height;
        if (contentContainer.anchoredPosition.y >= finishPosition)
        {
            isScrolling = false;
            OnCreditsFinished();
        }
    }

    private void BuildCreditsUI()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (!string.IsNullOrEmpty(creditData.gameTitle))
        {
            TextMeshProUGUI titleInstance = Instantiate(titlePrefab, contentContainer);
            titleInstance.text = creditData.gameTitle;
        }

        foreach (var section in creditData.sections)
        {
            if (!string.IsNullOrEmpty(section.sectionTitle))
            {
                TextMeshProUGUI headerInstance = Instantiate(headerPrefab, contentContainer);
                headerInstance.text = section.sectionTitle;
            }

            foreach (var entry in section.entries)
            {
                TextMeshProUGUI entryInstance = Instantiate(entryPrefab, contentContainer);
                entryInstance.text = FormatEntryText(section.sectionType, entry);
            }
        }

        if (!string.IsNullOrEmpty(creditData.closingMessage))
        {
            TextMeshProUGUI closingInstance = Instantiate(headerPrefab, contentContainer);
            closingInstance.text = creditData.closingMessage;
        }

        if (creditData.showCopyright && !string.IsNullOrEmpty(creditData.copyrightText))
        {
            TextMeshProUGUI copyrightInstance = Instantiate(entryPrefab, contentContainer);
            copyrightInstance.text = creditData.copyrightText;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

        totalContentHeight = CalculateContentHeight();

        float startY = -viewportRect.rect.height;
        contentContainer.anchoredPosition = new Vector2(contentContainer.anchoredPosition.x, startY);
    }

    private float CalculateContentHeight()
    {
        float height = LayoutUtility.GetPreferredHeight(contentContainer);
        if (height > 0f) return height;

        float calculatedHeight = 0f;
        foreach (RectTransform child in contentContainer)
        {
            calculatedHeight += LayoutUtility.GetPreferredHeight(child);
        }

        VerticalLayoutGroup layoutGroup = contentContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            calculatedHeight += layoutGroup.padding.top + layoutGroup.padding.bottom;
            calculatedHeight += (contentContainer.childCount - 1) * layoutGroup.spacing;
        }

        return calculatedHeight;
    }

    private string FormatEntryText(CreditSectionType type, CreditEntry entry)
    {
        switch (type)
        {
            case CreditSectionType.HeaderAndNames:
                return string.IsNullOrEmpty(entry.secondaryText)
                    ? entry.primaryText
                    : $"{entry.primaryText}\n<color=#CCCCCC><size=100%>{entry.secondaryText}</size></color>";

            case CreditSectionType.Attribution:
                if (!string.IsNullOrEmpty(entry.sourceText))
                {
                    return $"{entry.primaryText} - {entry.secondaryText}\n<color=#AAAAAA><size=100%>{entry.sourceText}</size></color>";
                }
                return $"{entry.primaryText}\n<color=#AAAAAA><size=85%>{entry.secondaryText}</size></color>";

            default:
                return entry.primaryText;
        }
    }

    private IEnumerator StartCreditSequence()
    {
        yield return new WaitForSeconds(startDelay);

        isScrolling = true;

        if (fastForwardPrompt != null) fastForwardPrompt.SetActive(true);
        if (backPrompt != null) backPrompt.SetActive(false);
    }

    private void OnCreditsFinished()
    {
        if (fastForwardPrompt != null) fastForwardPrompt.SetActive(false);
        if (backPrompt != null) backPrompt.SetActive(true);

        StartCoroutine(EndSequenceRoutine());
    }

    private IEnumerator EndSequenceRoutine()
    {
        yield return new WaitForSeconds(endDelay);
    }
}