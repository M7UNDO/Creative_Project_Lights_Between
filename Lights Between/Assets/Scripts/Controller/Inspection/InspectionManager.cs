using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("Input Setup")]
    [SerializeField] private InputActionReference advanceDialogueAction;

    [Header("UI References")]
    [SerializeField] private CanvasGroup inspectionCanvasGroup;
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

    [Header("Camera & Focus Settings")]
    [SerializeField] private Transform mainCameraTransform;
    [SerializeField] private float cameraTransitionSpeed = 5f;

    private FPSController playerController;
    private InspectableObject currentObject;

    private Vector3 originalCamPosition;
    private Quaternion originalCamRotation;

    private int currentThoughtIndex;
    private bool isInspecting;
    private bool isTyping;
    private bool canAdvance;
    private bool isTransitioningCamera;

    private Coroutine typingCoroutine;
    private Coroutine ellipsisCoroutine;
    private WaitForSeconds typeSpeedWait;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        typeSpeedWait = new WaitForSeconds(typeSpeed);
        if (inspectionCanvasGroup != null) inspectionCanvasGroup.alpha = 0f;

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

    public void StartInspection(InspectableObject inspectable)
    {
        if (isInspecting || isTransitioningCamera) return;

        currentObject = inspectable;
        ClearDialogueUI();

        if (currentObject != null && currentObject.WorldUI != null)
        {
            currentObject.WorldUI.SetSystemDisabled(true);
        }

        playerController = FindFirstObjectByType<FPSController>();

        if (playerController != null)
        {
            playerController.SetInspectionMode(true);
        }

        originalCamPosition = mainCameraTransform.position;
        originalCamRotation = mainCameraTransform.rotation;

        currentThoughtIndex = 0;
        isInspecting = true;

        StartCoroutine(EnterInspectionRoutine());
    }

    private IEnumerator EnterInspectionRoutine()
    {
        isTransitioningCamera = true;

        Vector3 targetPos = currentObject.InspectCameraTransform.position;
        Quaternion targetRot = currentObject.InspectCameraTransform.rotation;

        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * cameraTransitionSpeed;
            mainCameraTransform.position = Vector3.Lerp(originalCamPosition, targetPos, elapsedTime);
            mainCameraTransform.rotation = Quaternion.Slerp(originalCamRotation, targetRot, elapsedTime);
            yield return null;
        }

        mainCameraTransform.position = targetPos;
        mainCameraTransform.rotation = targetRot;
        isTransitioningCamera = false;

        yield return StartCoroutine(FadeCanvasGroup(inspectionCanvasGroup, 0f, 1f, 0.3f));
        ShowNextThought();
    }

    private void OnAdvancePerformed(InputAction.CallbackContext context)
    {
        if (!isInspecting || isTransitioningCamera) return;

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (canAdvance)
        {
            ShowNextThought();
        }
    }

    private void ShowNextThought()
    {
        if (currentThoughtIndex >= currentObject.InternalThoughts.Length)
        {
            EndInspection();
            return;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeThoughtRoutine(currentObject.InternalThoughts[currentThoughtIndex]));
        currentThoughtIndex++;
    }

    private IEnumerator TypeThoughtRoutine(string thought)
    {
        isTyping = true;
        canAdvance = false;

        HideEllipsis();
        SetAdvancePromptAlpha(typingPromptAlpha);

        dialogueText.text = thought;
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

    private void EndInspection()
    {
        if (!isInspecting || isTransitioningCamera) return;
        StartCoroutine(ExitInspectionRoutine());
    }

    private IEnumerator ExitInspectionRoutine()
    {
        isTransitioningCamera = true;
        canAdvance = false;

        HideEllipsis();

        yield return StartCoroutine(FadeCanvasGroup(inspectionCanvasGroup, 1f, 0f, 0.25f));

        ClearDialogueUI();

        float elapsedTime = 0f;
        Vector3 startPos = mainCameraTransform.position;
        Quaternion startRot = mainCameraTransform.rotation;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * cameraTransitionSpeed;
            mainCameraTransform.position = Vector3.Lerp(startPos, originalCamPosition, elapsedTime);
            mainCameraTransform.rotation = Quaternion.Slerp(startRot, originalCamRotation, elapsedTime);
            yield return null;
        }

        mainCameraTransform.position = originalCamPosition;
        mainCameraTransform.rotation = originalCamRotation;

        isTransitioningCamera = false;
        isInspecting = false;

        if (playerController != null)
        {
            playerController.SetInspectionMode(false);
        }

        if (currentObject != null && currentObject.WorldUI != null)
        {
            currentObject.WorldUI.SetSystemDisabled(false);
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

        AudioClip clip = typeSoundClips[Random.Range(0, typeSoundClips.Length)];
        if (clip != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip);
        }
    }
}