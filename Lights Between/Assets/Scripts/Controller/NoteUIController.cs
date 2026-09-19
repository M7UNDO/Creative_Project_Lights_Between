using UnityEngine;
using TMPro;

public class NoteUIController : MonoBehaviour
{
    public static NoteUIController Instance { get; private set; }

    [SerializeField] private InputHandler _input;
    [SerializeField] private FPSController playerController;

    [Header("UI References")]
    [SerializeField] private GameObject notePanelUI;
    [SerializeField] private TextMeshProUGUI noteText;

    [Header("Objective Activation (Scenario A)")]
    [SerializeField] private ObjectiveSO objectiveToActivate;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openNoteSound;
    [SerializeField] private AudioClip closeNoteSound;

    public static bool isNoteOpen { get; private set; }

    public bool IsNoteOpen => isNoteOpen;

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

    private void Update()
    {
        if (isNoteOpen && _input != null && _input.close)
        {
            CloseNote();
            _input.close = false;
        }
    }

    public void OpenNote(NoteData data)
    {
        if (data == null || notePanelUI == null || noteText == null) return;

        noteText.text = data.NoteContent;
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
        if (hasTriggeredObjective) return;

        if (ObjectiveManager.Instance != null && objectiveToActivate != null)
        {
            ObjectiveManager.Instance.ActivateObjective(objectiveToActivate);
            hasTriggeredObjective = true;
        }
    }
}