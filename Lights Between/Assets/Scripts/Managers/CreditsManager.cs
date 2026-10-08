using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private float endDelay = 1.5f;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference speedUpInput;

    [Header("Managers")]
    [SerializeField] private MainManager mainManager;

    private float currentSpeed;
    private bool isScrolling = false;
    private Coroutine scrollCoroutine;

    private void Awake()
    {
        if (mainManager == null)
        {
            mainManager = FindFirstObjectByType<MainManager>();
        }
    }

    private void OnEnable()
    {
        if (speedUpInput != null && speedUpInput.action != null)
        {
            speedUpInput.action.Enable();
        }

        BuildCreditsUI();
        ResetScrollPosition();

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }
        scrollCoroutine = StartCoroutine(StartCreditSequence());
    }

    private void OnDisable()
    {
        if (speedUpInput != null && speedUpInput.action != null)
        {
            speedUpInput.action.Disable();
        }

        isScrolling = false;
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }
    }

    private void Update()
    {
        if (!isScrolling) return;

        bool isSpeedingUp = speedUpInput != null && speedUpInput.action != null && speedUpInput.action.IsPressed();
        currentSpeed = isSpeedingUp ? scrollSpeed * fastForwardMultiplier : scrollSpeed;

        contentContainer.anchoredPosition += Vector2.up * (currentSpeed * Time.unscaledDeltaTime);

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

        if (creditData == null) return;

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
    }

    private void ResetScrollPosition()
    {
        if (viewportRect == null || contentContainer == null) return;
        float startY = -viewportRect.rect.height;
        contentContainer.anchoredPosition = new Vector2(contentContainer.anchoredPosition.x, startY);
    }

    private string FormatEntryText(CreditSectionType type, CreditEntry entry)
    {
        switch (type)
        {
            case CreditSectionType.Model3D:
                string license = string.IsNullOrEmpty(entry.licenseText) ? "CC BY 4.0" : entry.licenseText;
                return $"{entry.primaryText} - {entry.secondaryText}\nSketchfab - {license}";

            case CreditSectionType.Music:
                return $"{entry.primaryText}\nby {entry.secondaryText} via Pixabay";

            case CreditSectionType.Standard:
            default:
                if (string.IsNullOrEmpty(entry.secondaryText))
                {
                    return entry.primaryText;
                }
                return $"{entry.primaryText}\n{entry.secondaryText}";
        }
    }

    private IEnumerator StartCreditSequence()
    {
        isScrolling = false;
        yield return new WaitForSecondsRealtime(startDelay);
        isScrolling = true;
    }

    private void OnCreditsFinished()
    {
        StartCoroutine(EndCreditSequence());
    }

    private IEnumerator EndCreditSequence()
    {
        yield return new WaitForSecondsRealtime(endDelay);

        if (mainManager != null)
        {
            mainManager.ReturnToMainMenu();
        }
        else;
    }
}