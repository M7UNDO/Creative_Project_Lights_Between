using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public abstract class BaseOpenable : MonoBehaviour, IInteractable
{
    [Header("State Settings")]
    public bool isOpen = false;
    [SerializeField] protected float smoothSpeed = 1.0f;
    [SerializeField] private string customObjectName = "Object";

    [Header("Audio Settings")]
    [SerializeField] private AudioClip openSFX;
    [SerializeField] private AudioClip closeSFX;

    protected AudioSource audioSource;
    protected Coroutine animationCoroutine;

    public string promptMessage { get; private set; }

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        UpdatePromptMessage();
    }

    public virtual void Interact()
    {
        isOpen = !isOpen;
        UpdatePromptMessage();
        PlayInteractionSFX();

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateTransition());
    }

    private void UpdatePromptMessage()
    {
        promptMessage = isOpen ? $"Close {customObjectName}" : $"Open {customObjectName}";
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

    protected abstract IEnumerator AnimateTransition();
}