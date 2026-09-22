using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class NoteUIController : MonoBehaviour
{
    public static NoteUIController Instance { get; private set; }

    [SerializeField] private InputHandler _input;
    [SerializeField] private FPSController playerController;

    [Header("Input Action References")]
    [SerializeField] private InputActionReference nextPageAction;
    [SerializeField] private InputActionReference previousPageAction;

    [Header("UI References")]
    [SerializeField] private GameObject notePanelUI;
    [SerializeField] private TextMeshProUGUI noteText;
    [SerializeField] private TextMeshProUGUI pageNumberText;

    [Header("Objective Activation (Scenario A)")]
    [SerializeField] private ObjectiveSO objectiveToActivate;

    [Header("Fridge / Door Lock Reference")]
    [SerializeField] private BaseOpenable fridgeToUnlock;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openNoteSound;
    [SerializeField] private AudioClip closeNoteSound;
    [SerializeField] private AudioClip pageTurnSound;

    public static bool isNoteOpen { get; private set; }
    public bool IsNoteOpen => isNoteOpen;

    private NoteData currentNoteData;
    private int currentPageIndex = 0;
    private bool hasTriggeredObjective;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (notePanelUI != null)
        {
            notePanelUI.SetActive(false);
        }
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FPSController>();
        }
    }

    private void OnEnable()
    {
        if (nextPageAction != null)
        {
            nextPageAction.action.performed += OnNextPagePerformed;
            nextPageAction.action.Enable();
        }

        if (previousPageAction != null)
        {
            previousPageAction.action.performed += OnPreviousPagePerformed;
            previousPageAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (nextPageAction != null)
        {
            nextPageAction.action.performed -= OnNextPagePerformed;
            nextPageAction.action.Disable();
        }

        if (previousPageAction != null)
        {
            previousPageAction.action.performed -= OnPreviousPagePerformed;
            previousPageAction.action.Disable();
        }
    }

    private void Update()
    {
        if (isNoteOpen && _input != null && _input.close)
        {
            CloseNote();
            _input.close = false;
        }
    }

    private void OnNextPagePerformed(InputAction.CallbackContext context)
    {
        if (!isNoteOpen || currentNoteData == null) return;

        if (currentPageIndex < currentNoteData.TotalPages - 1)
        {
            currentPageIndex++;
            UpdateNoteDisplay();
            PlayPageTurnSFX();
        }
    }

    private void OnPreviousPagePerformed(InputAction.CallbackContext context)
    {
        if (!isNoteOpen || currentNoteData == null) return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateNoteDisplay();
            PlayPageTurnSFX();
        }
    }

    public void OpenNote(NoteData data)
    {
        if (data == null || notePanelUI == null || noteText == null) return;

        currentNoteData = data;
        currentPageIndex = 0;

        UpdateNoteDisplay();

        notePanelUI.SetActive(true);
        isNoteOpen = true;

        if (playerController != null)
        {
            playerController.EvaluateHUDState();
        }

        if (audioSource != null && openNoteSound != null)
        {
            audioSource.PlayOneShot(openNoteSound);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(true);
        }
    }

    private void UpdateNoteDisplay()
    {
        if (currentNoteData == null) return;

        if (noteText != null)
        {
            noteText.text = currentNoteData.GetPageContent(currentPageIndex);
        }

        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex + 1} / {currentNoteData.TotalPages}";
        }
    }

    private void PlayPageTurnSFX()
    {
        if (audioSource != null && pageTurnSound != null)
        {
            audioSource.PlayOneShot(pageTurnSound);
        }
    }

    public void CloseNote()
    {
        if (!isNoteOpen) return;

        if (notePanelUI != null)
        {
            notePanelUI.SetActive(false);
        }

        isNoteOpen = false;

        if (playerController != null)
        {
            playerController.EvaluateHUDState();
        }

        if (audioSource != null && closeNoteSound != null)
        {
            audioSource.PlayOneShot(closeNoteSound);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(false);
        }

        OnCloseNoteUI();
    }

    private void OnCloseNoteUI()
    {
        if (currentNoteData != null && currentNoteData.TriggersObjective)
        {
            if (!hasTriggeredObjective)
            {
                if (fridgeToUnlock != null)
                {
                    fridgeToUnlock.SetLockedState(false);
                }

                if (ObjectiveManager.Instance != null && objectiveToActivate != null)
                {
                    ObjectiveManager.Instance.ActivateObjective(objectiveToActivate);
                    hasTriggeredObjective = true;
                }
            }
        }

        currentNoteData = null;
    }
}