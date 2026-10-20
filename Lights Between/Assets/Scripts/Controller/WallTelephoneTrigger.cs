using System.Collections;
using UnityEngine;

public class WallTelephoneTrigger : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string promptMessage = "Dial Police";

    [Header("Narrative/Objective Lock")]
    [SerializeField] private ObjectiveSO requiredObjective;
    [SerializeField] private DialogueSO lockedDialogue;

    [Header("Narrative & Objective Progression")]
    [SerializeField] private ObjectiveSO objectiveToComplete;
    [SerializeField] private ObjectiveSO objectiveToActivate;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip dialSFX;
    [SerializeField] private AudioClip ringSFX;
    [SerializeField] private AudioClip deadToneSFX;

    [Header("Dialogue Narrative")]
    [SerializeField] private DialogueSO phoneCallDialogue;

    private bool hasBeenUsed = false;

    public string PromptMessage => IsTelephoneUnlocked() ? promptMessage : "";

    public bool IsTelephoneUnlocked()
    {
        if (hasBeenUsed) return false;

        if (requiredObjective == null) return true;

        if (ObjectiveManager.Instance != null)
        {
            return ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) == ObjectiveStatus.Active;
        }

        return false;
    }

    public void Interact()
    {
        if (!IsTelephoneUnlocked())
        {
            if (lockedDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(lockedDialogue);
            }
            return;
        }

        StartCoroutine(ExecuteTelephoneSequence());
    }

    private IEnumerator ExecuteTelephoneSequence()
    {
        hasBeenUsed = true;

        FPSController player = FindFirstObjectByType<FPSController>();

        if (player != null)
        {
            player.SetDialogueMode(true);
        }

        if (phoneAudioSource != null)
        {
            if (dialSFX != null)
            {
                phoneAudioSource.PlayOneShot(dialSFX);
                yield return new WaitForSeconds(dialSFX.length);
            }

            if (ringSFX != null)
            {
                phoneAudioSource.clip = ringSFX;
                phoneAudioSource.loop = false;
                phoneAudioSource.Play();
                yield return new WaitForSeconds(ringSFX.length);
            }

            if (deadToneSFX != null)
            {
                phoneAudioSource.clip = deadToneSFX;
                phoneAudioSource.loop = false;
                phoneAudioSource.Play();
                yield return new WaitForSeconds(deadToneSFX.length);
            }
        }

        if (ObjectiveManager.Instance != null)
        {
            if (objectiveToComplete != null)
            {
                ObjectiveManager.Instance.CompleteObjective(objectiveToComplete.ID);
            }

            if (objectiveToActivate != null)
            {
                ObjectiveManager.Instance.ActivateObjective(objectiveToActivate);
            }
        }

        if (DialogueManager.Instance != null && phoneCallDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(phoneCallDialogue);

            while (DialogueManager.Instance.IsDialogueActive)
            {
                yield return null;
            }
        }

        if (player != null)
        {
            player.SetDialogueMode(false);
        }
    }
}