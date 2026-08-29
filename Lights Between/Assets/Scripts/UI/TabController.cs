using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TabController : MonoBehaviour
{
    [Header("Tabs & Pages")]
    [SerializeField] private Selectable[] tabButtons;
    [SerializeField] private GameObject[] pages;

    [Header("Tab Text Colours")]
    [SerializeField] private Color selectedTextColour = Color.white;
    [SerializeField] private Color deselectedTextColour = Color.black;

    [Header("Settings")]
    [SerializeField] private bool startWithActiveTab = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSFX;

    private TextMeshProUGUI[] tabTexts;
    private int currentTab = -1;
    private InputActionReference navigationActionRef;

    private void Awake()
    {
        if (tabButtons == null || tabButtons.Length == 0)
        {
            tabButtons = GetComponentsInChildren<Selectable>(true);
        }

        tabTexts = new TextMeshProUGUI[tabButtons.Length];

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                tabTexts[i] = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }

    public void InitializeTabNavigation(InputActionReference actionRef)
    {
        if (navigationActionRef != null)
        {
            navigationActionRef.action.performed -= OnTabNavigationPerformed;
        }

        navigationActionRef = actionRef;

        if (navigationActionRef != null)
        {
            navigationActionRef.action.Enable();
            navigationActionRef.action.performed += OnTabNavigationPerformed;
        }
    }

    private void OnDestroy()
    {
        if (navigationActionRef != null)
        {
            navigationActionRef.action.performed -= OnTabNavigationPerformed;
        }
    }

    private void Start()
    {
        if (startWithActiveTab && tabButtons.Length > 0)
        {
            ActivateTab(0);
        }
    }

    private void OnTabNavigationPerformed(InputAction.CallbackContext context)
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        bool interactingWithTabs = false;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null && tabButtons[i].gameObject == currentSelected)
            {
                interactingWithTabs = true;
                break;
            }
        }

        if (!interactingWithTabs) return;

        float value = context.ReadValue<float>();
        if (value == 0) return;

        int nextTab = currentTab + (value > 0 ? 1 : -1);

        if (nextTab >= tabButtons.Length) nextTab = 0;
        if (nextTab < 0) nextTab = tabButtons.Length - 1;

        if (tabButtons[nextTab] != null)
        {
            EventSystem.current.SetSelectedGameObject(tabButtons[nextTab].gameObject);
            ActivateTab(nextTab);
        }
    }

    public void ActivateTab(int tabNo)
    {
        if (!IsValidTab(tabNo)) return;

        if (tabNo != currentTab)
        {
            PlaySFX(clickSFX);
        }

        currentTab = tabNo;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            bool isSelected = (i == tabNo);

            // Handle page activation
            if (i < pages.Length && pages[i] != null)
            {
                pages[i].SetActive(isSelected);
            }

            // Update Tab Text Colors based on active state
            if (tabTexts[i] != null)
            {
                tabTexts[i].color = isSelected ? selectedTextColour : deselectedTextColour;
            }

            // Sync HighlightButton's selection state safely via BaseEventData
            if (tabButtons[i] != null)
            {
                if (isSelected)
                {
                    ExecuteEvents.Execute(tabButtons[i].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.selectHandler);
                }
                else
                {
                    ExecuteEvents.Execute(tabButtons[i].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.deselectHandler);
                }
            }
        }

        // Drop focus into the active page's first selectable element
        if (pages[tabNo] != null)
        {
            FocusFirstElementInPage(pages[tabNo]);
        }
    }

    public bool TryGoBackToTab()
    {
        if (EventSystem.current == null || currentTab < 0 || currentTab >= tabButtons.Length)
            return false;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        // Check if focus is already on a tab button
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null && tabButtons[i].gameObject == currentSelected)
            {
                return false;
            }
        }

        // Focus was inside a page layer; return focus back up to the active tab button
        if (tabButtons[currentTab] != null)
        {
            EventSystem.current.SetSelectedGameObject(tabButtons[currentTab].gameObject);

            // Re-trigger select handler so HighlightButton updates state cleanly
            ExecuteEvents.Execute(tabButtons[currentTab].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.selectHandler);
            return true;
        }

        return false;
    }

    private void FocusFirstElementInPage(GameObject page)
    {
        Selectable firstSelectable = page.GetComponentInChildren<Selectable>();
        if (firstSelectable != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private bool IsValidTab(int tabNo)
    {
        return tabButtons != null && tabNo >= 0 && tabNo < tabButtons.Length;
    }
}