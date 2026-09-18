using UnityEngine;
using TMPro;

public class NoteUIController : MonoBehaviour
{
    public static NoteUIController Instance { get; private set; }
    [SerializeField] private InputHandler _input;

    [Header("UI References")]
    [SerializeField] private GameObject notePanelUI;
    [SerializeField] private TextMeshProUGUI noteText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openNoteSound;
    [SerializeField] private AudioClip closeNoteSound;

    public static bool isNoteOpen { get; private set; }

    public bool IsNoteOpen => isNoteOpen;

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

    private void Update()
    {
        if (isNoteOpen && _input.cancel)
        {
            CloseNote();
            _input.cancel = false;
        }
    }

    public void OpenNote(NoteData data)
    {
        if (data == null || notePanelUI == null || noteText == null) return;

        noteText.text = data.NoteContent;
        notePanelUI.SetActive(true);
        isNoteOpen = true;

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

        if (audioSource != null && closeNoteSound != null)
        {
            audioSource.PlayOneShot(closeNoteSound);
        }

        if (VolumeBlurController.Instance != null)
        {
            VolumeBlurController.Instance.SetMenuBlurState(false);
        }
    }
}