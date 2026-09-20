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

    [Header("Optional Objectives/Events")]
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string interactionEventID;

    private bool isOccupied;

    private void Start()
    {
        if (playerController != null) return;

        playerController = Object.FindFirstObjectByType<FPSController>();
    }

    public void Interact()
    {
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
        FPSController player = FindFirstObjectByType<FPSController>();
        if (player == null) return;

        if (ScreenFadeController.Instance != null)
        {
            ScreenFadeController.Instance.DoBlinkTransition(() =>
            {
                player.SetSeatedState(true, sitTargetPosition, lookAtTarget, this);
                isOccupied = true;

                if (interactionChannel != null && !string.IsNullOrEmpty(interactionEventID))
                {
                    interactionChannel.Raise(interactionEventID);
                }
            }, fadeDuration);
        }
        else
        {
            player.SetSeatedState(true, sitTargetPosition, lookAtTarget, this);
            isOccupied = true;
        }

        StartCoroutine(DisplayUI(true, 3f));
    }

    public void RequestStandUp()
    {
        if (!isOccupied) return;

        FPSController player = FindFirstObjectByType<FPSController>();
        if (player == null) return;

        if (ScreenFadeController.Instance != null)
        {
            ScreenFadeController.Instance.DoBlinkTransition(() =>
            {
                player.SetSeatedState(false);
                if (standUpPosition != null)
                {
                    player.Teleport(standUpPosition.position, standUpPosition.rotation);
                }
                isOccupied = false;
            }, fadeDuration);
        }
        else
        {
            player.SetSeatedState(false);
            if (standUpPosition != null)
            {
                player.Teleport(standUpPosition.position, standUpPosition.rotation);
            }
            isOccupied = false;
        }

        StartCoroutine(DisplayUI(false, 1f));


    }

    public string GetPromptMessage()
    {
        return isOccupied ? standPrompt : sitPrompt;
    }

    IEnumerator DisplayUI(bool display, float transition)
    {
        yield return new WaitForSeconds(transition);

        if(playerController.PosepromptUI != null)
        {
            playerController.PosepromptUI.SetActive(display);
        }
        
    }
}