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

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float fastForwardMultiplier = 3f;
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float endDelay = 2f;

    [Header("Input")]
    [SerializeField] private InputActionReference fastForwardInput;

    private float currentSpeed;
    private bool isScrolling = false;

    private void OnEnable()
    {
        if (fastForwardInput != null && fastForwardInput.action != null)
        {
            fastForwardInput.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fastForwardInput != null && fastForwardInput.action != null)
        {
            fastForwardInput.action.Disable();
        }
    }

    private void Start()
    {
        BuildCreditsUI();
        StartCoroutine(StartCreditSequence());
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

        float finishPosition = contentContainer.rect.height + viewportRect.rect.height;
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

        float startY = -viewportRect.rect.height;
        contentContainer.anchoredPosition = new Vector2(contentContainer.anchoredPosition.x, startY);
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
                    return $"{entry.primaryText} – {entry.secondaryText}\n<color=#AAAAAA><size=100%>{entry.sourceText}</size></color>";
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
    }

    private void OnCreditsFinished()
    {
        StartCoroutine(EndSequenceRoutine());
    }

    private IEnumerator EndSequenceRoutine()
    {
        yield return new WaitForSeconds(endDelay);
    }
}