using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FrontDoorEncounterTrigger : MonoBehaviour
{
    [Header("Audio SFX")]
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip initialSoftKnockSFX;
    [SerializeField] private AudioClip heavyBangingSFX;

    [Header("Dialogue Sequences")]
    [SerializeField] private DialogueSO initialDeceptiveDialogue;
    [SerializeField] private DialogueSO creatureThreatDialogue;

    [Header("Objectives")]
    [SerializeField] private ObjectiveSO investigateKnockObjective;
    [SerializeField] private ObjectiveSO nextStoryObjective;

    private BoxCollider triggerCollider;
    private bool hasTriggered;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
    }

    public void TriggerInitialKnock()
    {
        if (doorAudioSource != null && initialSoftKnockSFX != null)
        {
            doorAudioSource.PlayOneShot(initialSoftKnockSFX);
        }

        if (investigateKnockObjective != null && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ActivateObjective(investigateKnockObjective);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !other.CompareTag("Player")) return;

        hasTriggered = true;
        if (triggerCollider != null) triggerCollider.enabled = false;

        StartCoroutine(ExecuteFrontDoorSequence());
    }

    private IEnumerator ExecuteFrontDoorSequence()
    {
        if (ObjectiveManager.Instance != null && investigateKnockObjective != null)
        {
            ObjectiveManager.Instance.CompleteObjective(investigateKnockObjective.ID);
        }

        if (DialogueManager.Instance != null && initialDeceptiveDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(initialDeceptiveDialogue);

            while (DialogueManager.Instance.IsDialogueActive)
            {
                yield return null;
            }
        }

        yield return new WaitForSeconds(1.0f);

        if (doorAudioSource != null && initialSoftKnockSFX != null)
        {
            doorAudioSource.PlayOneShot(initialSoftKnockSFX);
            yield return new WaitForSeconds(initialSoftKnockSFX.length + 0.5f);
        }

        if (doorAudioSource != null && heavyBangingSFX != null)
        {
            doorAudioSource.PlayOneShot(heavyBangingSFX);
            yield return new WaitForSeconds(heavyBangingSFX.length + 0.3f);
        }

        if (DialogueManager.Instance != null && creatureThreatDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(creatureThreatDialogue);

            while (DialogueManager.Instance.IsDialogueActive)
            {
                yield return null;
            }
        }

        if (nextStoryObjective != null && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ActivateObjective(nextStoryObjective);
        }
    }
}