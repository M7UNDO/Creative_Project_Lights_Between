using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Input Setup")]
    [SerializeField] private InputActionReference advanceDialogueAction;

    [Header("UI References")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text ellipsisText;
    [SerializeField] private CanvasGroup advancePromptCanvasGroup;

    [Header("Prompt Opacity Settings")]
    [SerializeField] private float typingPromptAlpha = 0.3f;
    [SerializeField] private float idlePromptAlpha = 1.0f;

    [Header("Typing & Audio")]
    [SerializeField] private float typeSpeed = 0.035f;
    [SerializeField] private float ellipsisFlashSpeed = 0.4f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] typeSoundClips;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;

    private DialogueSO currentDialogue;
    private int currentLineIndex;
    private bool isDialogueActive;
    private bool isTyping;
    private bool canAdvance;

    private Coroutine typingCoroutine;
    private Coroutine ellipsisCoroutine;
    private WaitForSeconds typeSpeedWait;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        typeSpeedWait = new WaitForSeconds(typeSpeed);
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;

        ClearDialogueUI();
    }

    private void OnEnable()
    {
        if (advanceDialogueAction != null)
        {
            advanceDialogueAction.action.Enable();
            advanceDialogueAction.action.performed += OnAdvancePerformed;
        }
    }

    private void OnDisable()
    {
        if (advanceDialogueAction != null)
        {
            advanceDialogueAction.action.performed -= OnAdvancePerformed;
            advanceDialogueAction.action.Disable();
        }
    }

    public void StartDialogue(DialogueSO dialogue)
    {
        if (isDialogueActive || dialogue == null || dialogue.Lines == null || dialogue.Lines.Length == 0) return;

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;

        OnDialogueStarted?.Invoke();

        StartCoroutine(EnterDialogueRoutine());
    }

    private IEnumerator EnterDialogueRoutine()
    {
        yield return StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 0.25f));
        ShowNextLine();
    }

    private void OnAdvancePerformed(InputAction.CallbackContext context)
    {
        if (!isDialogueActive) return;

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (canAdvance)
        {
            ShowNextLine();
        }
    }

    private void ShowNextLine()
    {
        if (currentLineIndex >= currentDialogue.Lines.Length)
        {
            EndDialogue();
            return;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLineRoutine(currentDialogue.Lines[currentLineIndex]));
        currentLineIndex++;
    }

    private IEnumerator TypeLineRoutine(string line)
    {
        isTyping = true;
        canAdvance = false;

        HideEllipsis();
        SetAdvancePromptAlpha(typingPromptAlpha);

        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int totalChars = dialogueText.textInfo.characterCount;

        for (int i = 0; i <= totalChars; i++)
        {
            if (!isTyping) break;

            dialogueText.maxVisibleCharacters = i;

            if (i > 0 && i <= totalChars)
            {
                char c = dialogueText.textInfo.characterInfo[i - 1].character;
                if (!char.IsWhiteSpace(c)) PlayTypeSound();
            }

            yield return typeSpeedWait;
        }

        dialogueText.maxVisibleCharacters = totalChars;
        isTyping = false;
        canAdvance = true;

        SetAdvancePromptAlpha(idlePromptAlpha);

        if (ellipsisText != null)
        {
            if (ellipsisCoroutine != null) StopCoroutine(ellipsisCoroutine);
            ellipsisCoroutine = StartCoroutine(FlashEllipsisRoutine());
        }
    }

    private void SkipTyping()
    {
        isTyping = false;
        SetAdvancePromptAlpha(idlePromptAlpha);
    }

    private void EndDialogue()
    {
        StartCoroutine(ExitDialogueRoutine());
    }

    private IEnumerator ExitDialogueRoutine()
    {
        canAdvance = false;
        HideEllipsis();

        yield return StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, 0.25f));

        ClearDialogueUI();

        DialogueSO finishedDialogue = currentDialogue;
        currentDialogue = null;
        isDialogueActive = false;

        OnDialogueEnded?.Invoke();

        if (finishedDialogue != null)
        {
            if (finishedDialogue.OnDialogueCompletedEvent != null)
            {
                finishedDialogue.OnDialogueCompletedEvent.Raise();
            }

            if (finishedDialogue.ObjectiveToActivateOnComplete != null && ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.ActivateObjective(finishedDialogue.ObjectiveToActivateOnComplete);
            }
        }
    }

    private void ClearDialogueUI()
    {
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = 0;
        }
        HideEllipsis();
        SetAdvancePromptAlpha(0f);
    }

    private void HideEllipsis()
    {
        if (ellipsisCoroutine != null) StopCoroutine(ellipsisCoroutine);
        if (ellipsisText != null)
        {
            ellipsisText.maxVisibleCharacters = 0;
        }
    }

    private void SetAdvancePromptAlpha(float alpha)
    {
        if (advancePromptCanvasGroup != null)
        {
            advancePromptCanvasGroup.alpha = alpha;
        }
    }

    private IEnumerator FlashEllipsisRoutine()
    {
        var wait = new WaitForSeconds(ellipsisFlashSpeed);
        bool visible = true;

        while (true)
        {
            ellipsisText.maxVisibleCharacters = visible ? ellipsisText.textInfo.characterCount : 0;
            visible = !visible;
            yield return wait;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
    {
        if (group == null) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }

        group.alpha = endAlpha;
    }

    private void PlayTypeSound()
    {
        if (audioSource == null || typeSoundClips == null || typeSoundClips.Length == 0) return;

        AudioClip clip = typeSoundClips[UnityEngine.Random.Range(0, typeSoundClips.Length)];
        if (clip != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip);
        }
    }
}