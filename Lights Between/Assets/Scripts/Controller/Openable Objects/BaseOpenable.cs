using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public abstract class BaseOpenable : MonoBehaviour, IInteractable
{
    [Header("Lock Settings")]
    [SerializeField] protected bool isLocked = false;
    [SerializeField] protected string lockedPrompt = "Locked";

    [Header("State Settings")]
    public bool isOpen = false;
    [SerializeField] protected float smoothSpeed = 1.0f;
    [SerializeField] private string customObjectName = "Object";

    [Header("Audio Settings")]
    [SerializeField] private AudioClip openSFX;
    [SerializeField] private AudioClip closeSFX;
    [SerializeField] private AudioClip lockedSFX;

    protected AudioSource audioSource;
    protected Coroutine animationCoroutine;

    public string promptMessage { get; protected set; }

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        UpdatePromptMessage();
    }

    public virtual bool CanInteract()
    {
        return !isLocked && !NoteUIController.isNoteOpen;
    }

    public void SetLockedState(bool locked)
    {
        isLocked = locked;
        UpdatePromptMessage();
    }

    public virtual void Interact()
    {
        if (isLocked)
        {
            PlayLockedSFX();
            return;
        }

        if (!CanInteract()) return;

        isOpen = !isOpen;
        UpdatePromptMessage();
        PlayInteractionSFX();

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateTransition());
    }

    public void UpdatePromptMessage()
    {
        if (isLocked)
        {
            promptMessage = lockedPrompt;
            return;
        }

        if (NoteUIController.isNoteOpen)
        {
            promptMessage = string.Empty;
            return;
        }

        promptMessage = isOpen ? "Close" : "Open";
    }

    private void PlayInteractionSFX()
    {
        if (audioSource != null)
        {
            AudioClip clipToPlay = isOpen ? openSFX : closeSFX;
            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.Play();
            }
        }
    }

    private void PlayLockedSFX()
    {
        if (audioSource != null && lockedSFX != null)
        {
            audioSource.PlayOneShot(lockedSFX);
        }
    }

    protected abstract IEnumerator AnimateTransition();
}