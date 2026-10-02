using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TabController : MonoBehaviour
{
    [Header("Tabs & Pages")]
    [SerializeField] private Image[] tabButtons;
    [SerializeField] private GameObject[] pages;

    [Header("Tab Colours")]
    [SerializeField] private Color selectedTabColour = Color.black;
    [SerializeField] private Color selectedTextColour = Color.white;
    [SerializeField] private Color deselectedTabColour = Color.white;
    [SerializeField] private Color deselectedTextColour = Color.black;

    [Header("Settings")]
    [SerializeField] private bool startWithActiveTab = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSFX;

    private TextMeshProUGUI[] tabTexts;
    private int currentTab = -1;
    private InputAction boundNextAction;
    private InputAction boundPreviousAction;

    private void Awake()
    {
        if (tabButtons == null || tabButtons.Length == 0)
        {
            tabButtons = GetComponentsInChildren<Image>(true);
        }

        tabTexts = new TextMeshProUGUI[tabButtons.Length];

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                tabTexts[i] = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();

                RegisterPointerClick(tabButtons[i].gameObject, i);
            }
        }
    }

    private void RegisterPointerClick(GameObject tabObject, int tabIndex)
    {
        EventTrigger trigger = tabObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = tabObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener((data) => { ActivateTab(tabIndex); });
        trigger.triggers.Add(entry);
    }

    private void Start()
    {
        if (startWithActiveTab && tabButtons.Length > 0)
        {
            ActivateTab(0);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeActions();
    }

    public void InitializeTabNavigation(InputAction previousTabAction, InputAction nextTabAction)
    {
        UnsubscribeActions();

        boundPreviousAction = previousTabAction;
        boundNextAction = nextTabAction;

        if (boundPreviousAction != null)
        {
            boundPreviousAction.performed += OnPreviousTabPerformed;
        }

        if (boundNextAction != null)
        {
            boundNextAction.performed += OnNextTabPerformed;
        }
    }

    private void UnsubscribeActions()
    {
        if (boundPreviousAction != null)
        {
            boundPreviousAction.performed -= OnPreviousTabPerformed;
        }

        if (boundNextAction != null)
        {
            boundNextAction.performed -= OnNextTabPerformed;
        }
    }

    private void OnPreviousTabPerformed(InputAction.CallbackContext context)
    {
        CycleTab(-1);
    }

    private void OnNextTabPerformed(InputAction.CallbackContext context)
    {
        CycleTab(1);
    }

    private void CycleTab(int direction)
    {
        if (tabButtons == null || tabButtons.Length == 0) return;

        int nextTab = currentTab + direction;

        if (nextTab >= tabButtons.Length) nextTab = 0;
        if (nextTab < 0) nextTab = tabButtons.Length - 1;

        if (tabButtons[nextTab] != null)
        {
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

            if (i < pages.Length && pages[i] != null)
            {
                pages[i].SetActive(isSelected);
            }

            if (tabButtons[i] != null)
            {
                tabButtons[i].color = isSelected ? selectedTabColour : deselectedTabColour;
            }

            if (tabTexts[i] != null)
            {
                tabTexts[i].color = isSelected ? selectedTextColour : deselectedTextColour;
            }
        }

        if (pages[tabNo] != null)
        {
            FocusFirstElementInPage(pages[tabNo]);
        }
    }

    public bool TryGoBackToTab()
    {
        if (EventSystem.current == null || !IsValidTab(currentTab))
            return false;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null && tabButtons[i].gameObject == currentSelected)
            {
                return false;
            }
        }

        if (tabButtons[currentTab] != null)
        {
            EventSystem.current.SetSelectedGameObject(tabButtons[currentTab].gameObject);
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