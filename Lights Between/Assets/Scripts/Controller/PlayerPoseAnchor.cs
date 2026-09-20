using UnityEngine;

public class PlayerPoseAnchor : MonoBehaviour, IInteractable
{
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

    public void Interact()
    {
        FPSController player = FindFirstObjectByType<FPSController>();
        if (player == null) return;

        if (!isOccupied)
        {
            // Transition to sitting
            if (ScreenFadeController.Instance != null)
            {
                ScreenFadeController.Instance.DoBlinkTransition(() =>
                {
                    player.SetSeatedState(true, sitTargetPosition, lookAtTarget);
                    isOccupied = true;

                    if (interactionChannel != null && !string.IsNullOrEmpty(interactionEventID))
                    {
                        interactionChannel.Raise(interactionEventID);
                    }
                }, fadeDuration);
            }
            else
            {
                player.SetSeatedState(true, sitTargetPosition, lookAtTarget);
                isOccupied = true;
            }
        }
        else
        {
            // Transition to standing up
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
        }
    }

    public string GetPromptMessage()
    {
        return isOccupied ? standPrompt : sitPrompt;
    }
}