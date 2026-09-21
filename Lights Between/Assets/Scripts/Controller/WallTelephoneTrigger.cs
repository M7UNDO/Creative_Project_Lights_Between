using System.Collections;
using UnityEngine;

public class WallTelephoneTrigger : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string promptMessage = "Dial Police";
    [SerializeField] private bool isUnlocked = false;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip dialSFX;
    [SerializeField] private AudioClip ringSFX;
    [SerializeField] private AudioClip deadToneSFX;

    [Header("Dialogue Narrative")]
    [SerializeField] private DialogueSO phoneCallDialogue;

    public string PromptMessage => isUnlocked ? promptMessage : "";

    public void SetTelephoneUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;
    }

    public void Interact()
    {
        if (!isUnlocked) return;

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