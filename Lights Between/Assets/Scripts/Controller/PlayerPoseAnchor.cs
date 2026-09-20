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
    [SerializeField] private float uiDisplayDuration = 5.0f;
    [SerializeField] private float uiFadeSpeed = 2.0f;

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
    private CanvasGroup poseCanvasGroup;
    private Coroutine uiFadeCoroutine;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<FPSController>();
        }

        CacheCanvasGroup();
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

    public bool IsAnchorUnlocked()
    {
        // If no required objective is set, it's always accessible (like normal chairs)
        if (requiredObjective == null) return true;

        // Otherwise check if the required objective is active in the ObjectiveManager
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

        // Check if locked narratively
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
        else
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

                ShowPromptTimed();

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
            ShowPromptTimed();
        }
    }

    public void RequestStandUp()
    {
        if (isTransitioning || !isOccupied) return;

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<FPSController>();
            if (playerController == null) return;
        }

        isTransitioning = true;
        HidePromptImmediate();

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

    private void ShowPromptTimed()
    {
        if (poseCanvasGroup == null) return;

        if (uiFadeCoroutine != null)
        {
            StopCoroutine(uiFadeCoroutine);
        }

        uiFadeCoroutine = StartCoroutine(FadePromptRoutine());
    }

    private void HidePromptImmediate()
    {
        if (uiFadeCoroutine != null)
        {
            StopCoroutine(uiFadeCoroutine);
            uiFadeCoroutine = null;
        }

        if (poseCanvasGroup != null)
        {
            poseCanvasGroup.alpha = 0f;
        }
    }

    private IEnumerator FadePromptRoutine()
    {
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            poseCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.3f);
            yield return null;
        }
        poseCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(uiDisplayDuration);

        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * uiFadeSpeed;
            poseCanvasGroup.alpha = Mathf.Clamp01(1f - elapsed);
            yield return null;
        }
        poseCanvasGroup.alpha = 0f;
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