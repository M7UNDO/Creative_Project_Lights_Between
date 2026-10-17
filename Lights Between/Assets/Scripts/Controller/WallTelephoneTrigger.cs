using System.Collections;
using UnityEngine;

public class WallTelephoneTrigger : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string promptMessage = "Dial Police";
    [SerializeField] private bool isUnlocked = false;

    [Header("Narrative/Objective Lock")]
    [Tooltip("If assigned, the player can only interact when this objective is active.")]
    [SerializeField] private ObjectiveSO requiredObjective;
    [Tooltip("Optional dialogue to play if player tries to use this before it's unlocked.")]
    [SerializeField] private DialogueSO lockedDialogue;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip dialSFX;
    [SerializeField] private AudioClip ringSFX;
    [SerializeField] private AudioClip deadToneSFX;

    [Header("Dialogue Narrative")]
    [SerializeField] private DialogueSO phoneCallDialogue;

    public string PromptMessage => IsTelephoneUnlocked() ? promptMessage : string.Empty;

    public void SetTelephoneUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
    }

    public bool IsTelephoneUnlocked()
    {
        if (requiredObjective != null)
        {
            if (ObjectiveManager.Instance != null)
            {
                return ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) == ObjectiveStatus.Active;
            }
            return false;
        }

        return isUnlocked;
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
        isUnlocked = false;

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