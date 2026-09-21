using UnityEngine;
using System.Collections;

public class PlayerPoseAnchor : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private FPSController playerController;

    [Header("Pose Targets")]
    [SerializeField] private Transform sitTargetPosition;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Transform standUpPosition;

    [Header("Interaction Settings")]
    [SerializeField] private string sitPrompt = "Sit Down";
    [SerializeField] private string standPrompt = "Stand Up";
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float uiFadeSpeed = 4.0f;

    [Header("Narrative/Objective Lock")]
    [Tooltip("If assigned, the player can only interact when this objective is active.")]
    [SerializeField] private ObjectiveSO requiredObjective;
    [Tooltip("Optional dialogue to play if player tries to use this before it's unlocked.")]
    [SerializeField] private DialogueSO lockedDialogue;

    [Header("Optional Objectives/Events")]
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string interactionEventID;

    private bool isOccupied;
    private bool isTransitioning;
    private bool canStandUp = true;
    private CanvasGroup poseCanvasGroup;
    private Coroutine uiFadeCoroutine;

    public event System.Action OnPoseEntered;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<FPSController>();
        }

        CacheCanvasGroup();
        SubscribeToDialogueManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDialogueManager();
    }

    private void SubscribeToDialogueManager()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted += HandleDialogueStarted;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void UnsubscribeFromDialogueManager()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= HandleDialogueStarted;
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }
    }

    private void CacheCanvasGroup()
    {
        if (playerController != null && playerController.PosepromptUI != null)
        {
            poseCanvasGroup = playerController.PosepromptUI.GetComponent<CanvasGroup>();
            if (poseCanvasGroup == null)
            {
                poseCanvasGroup = playerController.PosepromptUI.AddComponent<CanvasGroup>();
            }

            poseCanvasGroup.alpha = 0f;
            playerController.PosepromptUI.SetActive(true);
        }
    }

    private void HandleDialogueStarted()
    {
        if (isOccupied)
        {
            FadePromptTo(0f);
        }
    }

    private void HandleDialogueEnded()
    {
        if (isOccupied && canStandUp)
        {
            FadePromptTo(1f);
        }
    }

    public void SetStandUpPermission(bool permit)
    {
        canStandUp = permit;
        if (!canStandUp)
        {
            FadePromptTo(0f);
        }
        else if (isOccupied && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
        {
            FadePromptTo(1f);
        }
    }

    public bool IsAnchorUnlocked()
    {
        if (requiredObjective == null) return true;

        if (ObjectiveManager.Instance != null)
        {
            return ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) == ObjectiveStatus.Active;
        }

        return false;
    }

    public void Interact()
    {
        if (isTransitioning) return;

        if (playerController != null && playerController.CurrentGrabbable != null)
        {
            return;
        }

        if (!IsAnchorUnlocked())
        {
            if (lockedDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(lockedDialogue);
            }
            return;
        }

        if (!isOccupied)
        {
            SitDown();
        }
        else if (canStandUp)
        {
            RequestStandUp();
        }
    }

    public void SitDown()
    {
        if (isTransitioning || isOccupied || !IsAnchorUnlocked()) return;

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<FPSController>();
            if (playerController == null) return;
        }

        if (playerController.CurrentGrabbable != null) return;

        CacheCanvasGroup();
        isTransitioning = true;

        if (ScreenFadeController.Instance != null)
        {
            ScreenFadeController.Instance.DoBlinkTransition(() =>
            {
                playerController.SetSeatedState(true, sitTargetPosition, lookAtTarget, this);
                isOccupied = true;
                isTransitioning = false;

                if (canStandUp && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
                {
                    FadePromptTo(1f);
                }

                OnPoseEntered?.Invoke();

                if (interactionChannel != null && !string.IsNullOrEmpty(interactionEventID))
                {
                    interactionChannel.Raise(interactionEventID);
                }
            }, fadeDuration);
        }
        else
        {
            playerController.SetSeatedState(true, sitTargetPosition, lookAtTarget, this);
            isOccupied = true;
            isTransitioning = false;
            if (canStandUp && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
            {
                FadePromptTo(1f);
            }
        }
    }

    public void RequestStandUp()
    {
        if (isTransitioning || !isOccupied || !canStandUp) return;

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<FPSController>();
            if (playerController == null) return;
        }

        isTransitioning = true;
        FadePromptTo(0f);

        if (ScreenFadeController.Instance != null)
        {
            ScreenFadeController.Instance.DoBlinkTransition(() =>
            {
                playerController.SetSeatedState(false);
                if (standUpPosition != null)
                {
                    playerController.Teleport(standUpPosition.position, standUpPosition.rotation);
                }
                isOccupied = false;
                isTransitioning = false;
            }, fadeDuration);
        }
        else
        {
            playerController.SetSeatedState(false);
            if (standUpPosition != null)
            {
                playerController.Teleport(standUpPosition.position, standUpPosition.rotation);
            }
            isOccupied = false;
            isTransitioning = false;
        }
    }

    public void FadePromptTo(float targetAlpha)
    {
        if (poseCanvasGroup == null) return;

        if (uiFadeCoroutine != null)
        {
            StopCoroutine(uiFadeCoroutine);
        }

        uiFadeCoroutine = StartCoroutine(FadePromptRoutine(targetAlpha));
    }

    private IEnumerator FadePromptRoutine(float targetAlpha)
    {
        float startAlpha = poseCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * uiFadeSpeed;
            poseCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed);
            yield return null;
        }

        poseCanvasGroup.alpha = targetAlpha;
        uiFadeCoroutine = null;
    }

    public string GetPromptMessage()
    {
        if (!isOccupied && playerController != null && playerController.CurrentGrabbable != null)
        {
            return "";
        }

        if (!IsAnchorUnlocked())
        {
            return "";
        }

        return isOccupied ? standPrompt : sitPrompt;
    }
}