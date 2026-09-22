using System.Collections;
using UnityEngine;

public class DoorSequenceController : MonoBehaviour
{
    [Header("Event Channels (Listeners)")]
    [SerializeField] private GameEventSO onPhoneCallEndedEvent;
    [SerializeField] private GameEventSO onFrontDoorReachedEvent;
    [SerializeField] private GameEventSO onDoorRefusalEndedEvent;
    [SerializeField] private StringGameEventSO interactionChannel;

    [Header("Doll Setup")]
    [SerializeField] private GameObject staticSammyDollInRoom;
    [SerializeField] private GameObject dynamicSammyDollPrefab;
    [SerializeField] private Transform dollSpawnPoint;

    [Header("Door Knock Sequence")]
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip softKnockSFX;
    [SerializeField] private AudioClip heavyBangSFX;
    [SerializeField] private string returnDollInteractionID = InteractionIDs.ReturnDoll;
    [SerializeField] private float reminderKnockInterval = 12.0f;

    [Header("Door Trigger")]
    [SerializeField] private GameObject dialogueTrigger;

    [Header("Phase 2 Dialogue")]
    [SerializeField] private DialogueSO creaturePhase2Dialogue;

    [Header("Objectives")]
    [SerializeField] private ObjectiveSO investigateKnockObjective;

    private bool hasSpawnedDoll;
    private bool hasReachedDoor;
    private bool hasHandledDoorRefusal;
    private Coroutine reminderKnockCoroutine;

    private void Awake()
    {
        if (dialogueTrigger != null)
        {
            dialogueTrigger.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (onPhoneCallEndedEvent != null)
        {
            onPhoneCallEndedEvent.RegisterListener(HandlePhoneCallEnded);
        }

        if (onFrontDoorReachedEvent != null)
        {
            onFrontDoorReachedEvent.RegisterListener(OnPlayerReachedFrontDoor);
        }

        if (onDoorRefusalEndedEvent != null)
        {
            onDoorRefusalEndedEvent.RegisterListener(HandleDoorRefusalEnded);
        }

        if (interactionChannel != null)
        {
            interactionChannel.RegisterListener(HandleInteractionEvent);
        }
    }

    private void OnDisable()
    {
        if (onPhoneCallEndedEvent != null)
        {
            onPhoneCallEndedEvent.UnregisterListener(HandlePhoneCallEnded);
        }

        if (onFrontDoorReachedEvent != null)
        {
            onFrontDoorReachedEvent.UnregisterListener(OnPlayerReachedFrontDoor);
        }

        if (onDoorRefusalEndedEvent != null)
        {
            onDoorRefusalEndedEvent.UnregisterListener(HandleDoorRefusalEnded);
        }

        if (interactionChannel != null)
        {
            interactionChannel.UnregisterListener(HandleInteractionEvent);
        }
    }

    private void HandlePhoneCallEnded()
    {
        if (hasSpawnedDoll) return;

        hasSpawnedDoll = true;

        if (staticSammyDollInRoom != null)
        {
            staticSammyDollInRoom.SetActive(false);
        }

        if (dynamicSammyDollPrefab != null && dollSpawnPoint != null)
        {
            Instantiate(dynamicSammyDollPrefab, dollSpawnPoint.position, dollSpawnPoint.rotation);
        }
    }

    private void HandleInteractionEvent(string interactionID)
    {
        if (interactionID == returnDollInteractionID)
        {
            if (reminderKnockCoroutine != null) StopCoroutine(reminderKnockCoroutine);
            reminderKnockCoroutine = StartCoroutine(ExecuteInitialDoorKnockSequence());
        }
    }

    private IEnumerator ExecuteInitialDoorKnockSequence()
    {
        if (dialogueTrigger != null)
        {
            dialogueTrigger.SetActive(true);
        }

        if (ObjectiveManager.Instance != null && investigateKnockObjective != null)
        {
            ObjectiveManager.Instance.ActivateObjective(investigateKnockObjective);
        }

        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.TransitionToState(MusicState.DoorKnockTension, 0.5f);
        }

        yield return new WaitForSeconds(1.0f);

        while (!hasReachedDoor)
        {
            if (doorAudioSource != null && softKnockSFX != null)
            {
                doorAudioSource.PlayOneShot(softKnockSFX);
            }

            yield return new WaitForSeconds(reminderKnockInterval);
        }
    }

    public void OnPlayerReachedFrontDoor()
    {
        hasReachedDoor = true;

        if (dialogueTrigger != null)
        {
            dialogueTrigger.SetActive(false);
        }

        if (reminderKnockCoroutine != null)
        {
            StopCoroutine(reminderKnockCoroutine);
            reminderKnockCoroutine = null;
        }
    }

    private void HandleDoorRefusalEnded()
    {
        if (hasHandledDoorRefusal) return;

        hasHandledDoorRefusal = true;
        OnPlayerReachedFrontDoor();
        StartCoroutine(ExecuteCreatureBangingSequence());
    }

    private IEnumerator ExecuteCreatureBangingSequence()
    {
        yield return new WaitForSeconds(1.5f);

        if (doorAudioSource != null && softKnockSFX != null)
        {
            doorAudioSource.PlayOneShot(softKnockSFX);
        }

        yield return new WaitForSeconds(2.0f);

        if (doorAudioSource != null && heavyBangSFX != null)
        {
            doorAudioSource.PlayOneShot(heavyBangSFX);
            yield return new WaitForSeconds(0.35f);
            doorAudioSource.PlayOneShot(heavyBangSFX);
            yield return new WaitForSeconds(0.35f);
            doorAudioSource.PlayOneShot(heavyBangSFX);
        }

        yield return new WaitForSeconds(1.2f);

        if (DialogueManager.Instance != null && creaturePhase2Dialogue != null)
        {
            DialogueManager.Instance.StartDialogue(creaturePhase2Dialogue);
        }
    }
}