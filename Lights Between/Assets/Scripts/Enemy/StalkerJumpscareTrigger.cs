using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(BoxCollider))]
public class StalkerJumpscareTrigger : MonoBehaviour
{
    [Header("State Settings")]
    [Tooltip("If false, player collisions are ignored until activated externally (e.g. via UnityEvent).")]
    [SerializeField] private bool isActiveOnStart = false;

    [Header("Creature Actor Settings")]
    [SerializeField] private GameObject creatureActor;
    [SerializeField] private Animator creatureAnimator;
    [SerializeField] private Transform creatureSpawnPoint;
    [SerializeField] private Transform creatureRetreatPoint;
    [SerializeField] private float lookAtDuration = 1.5f;
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Creature Animation Parameters")]
    [SerializeField] private string speedFloatName = "Speed";
    [SerializeField] private float walkSpeedPercent = 0.5f;
    [SerializeField] private float idleSpeedPercent = 0.0f;

    [Header("Creature Rigging & Look Target")]
    [SerializeField] private MultiAimConstraint creatureHeadAim;
    [SerializeField] private float rigLerpSpeed = 5f;

    [Header("Camera Focus Target")]
    [SerializeField] private Transform creatureLookTarget;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpscareSFX;
    [SerializeField] private AudioSource heartbeatAudioSource;

    [Header("Visual Effects")]
   // [SerializeField] private PlayerFearFeedback playerFeedbackController;
    [SerializeField] private float maxAberrationIntensity = 0.85f;
    [SerializeField] private float aberrationPulseSpeed = 3.0f;

    [Header("Narrative Reaction")]
    //[SerializeField] private DialogueSO policeCallReactionDialogue;
    [SerializeField] private WallTelephoneTrigger wallPhone;

    private BoxCollider triggerCollider;
    private bool hasTriggered;
    private Coroutine headAimCoroutine;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        SetTriggerState(isActiveOnStart);
        SetCreatureHeadAimWeightDirect(0f);
    }

    public void ActivateTrigger()
    {
        if (hasTriggered) return;
        SetTriggerState(true);
    }

    public void DeactivateTrigger()
    {
        SetTriggerState(false);
    }

    public void TriggerScare()
    {
        if (hasTriggered) return;

        FPSController player = FindFirstObjectByType<FPSController>();
        if (player != null)
        {
            ExecuteSequenceForPlayer(player);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        FPSController player = other.GetComponent<FPSController>();
        if (player != null)
        {
            ExecuteSequenceForPlayer(player);
        }
    }

    private void ExecuteSequenceForPlayer(FPSController player)
    {
        hasTriggered = true;
        SetTriggerState(false);
        StartCoroutine(ExecuteStalkerSequence(player));
    }

    private void SetTriggerState(bool enabledState)
    {
        if (triggerCollider != null)
        {
            triggerCollider.enabled = enabledState;
        }
    }

    private void SetCreatureHeadAimWeightDirect(float weight)
    {
        if (creatureHeadAim != null)
        {
            creatureHeadAim.weight = weight;
        }
    }

    private void LerpCreatureHeadAimWeight(float targetWeight)
    {
        if (headAimCoroutine != null)
        {
            StopCoroutine(headAimCoroutine);
        }
        headAimCoroutine = StartCoroutine(LerpHeadAimRoutine(targetWeight));
    }

    private IEnumerator LerpHeadAimRoutine(float targetWeight)
    {
        if (creatureHeadAim == null) yield break;

        while (Mathf.Abs(creatureHeadAim.weight - targetWeight) > 0.01f)
        {
            creatureHeadAim.weight = Mathf.Lerp(creatureHeadAim.weight, targetWeight, Time.deltaTime * rigLerpSpeed);
            yield return null;
        }

        creatureHeadAim.weight = targetWeight;
        headAimCoroutine = null;
    }

    private IEnumerator ExecuteStalkerSequence(FPSController fpsController)
    {
        if (creatureActor != null && creatureSpawnPoint != null)
        {
            creatureActor.SetActive(false);
            creatureActor.transform.SetPositionAndRotation(creatureSpawnPoint.position, creatureSpawnPoint.rotation);

            if (creatureAnimator != null)
            {
                creatureAnimator.Rebind();
                creatureAnimator.Update(0f);
                creatureAnimator.SetFloat(speedFloatName, idleSpeedPercent);
            }
        }

        if (fpsController != null)
        {
            fpsController.SetJumpScareMode(true);

            Vector3 lookPosition = creatureLookTarget != null
                ? creatureLookTarget.position
                : (creatureSpawnPoint != null ? creatureSpawnPoint.position : transform.position);

            fpsController.ForceLookAt(lookPosition);
        }

        if (audioSource != null && jumpscareSFX != null)
        {
            audioSource.PlayOneShot(jumpscareSFX);
        }

        if (heartbeatAudioSource != null)
        {
            heartbeatAudioSource.Play();
        }

        /*if (playerFeedbackController != null)
        {
            playerFeedbackController.StartPulse(maxAberrationIntensity, aberrationPulseSpeed);
        }*/

        if (creatureActor != null)
        {
            creatureActor.SetActive(true);
            SetCreatureHeadAimWeightDirect(1f);
        }

        yield return new WaitForSeconds(lookAtDuration);

       if (fpsController != null)
        {
            fpsController.SetJumpScareMode(false);
        }

        if (creatureActor != null && creatureRetreatPoint != null)
        {
            if (creatureAnimator != null)
            {
                creatureAnimator.SetFloat(speedFloatName, walkSpeedPercent);
            }

            Transform creatureTransform = creatureActor.transform;

            while (Vector3.Distance(creatureTransform.position, creatureRetreatPoint.position) > 0.1f)
            {
                creatureTransform.position = Vector3.MoveTowards(
                    creatureTransform.position,
                    creatureRetreatPoint.position,
                    moveSpeed * Time.deltaTime
                );

                if (fpsController != null)
                {
                    Vector3 directionToPlayer = fpsController.transform.position - creatureTransform.position;
                    directionToPlayer.y = 0f;

                    if (directionToPlayer.sqrMagnitude > 0.001f)
                    {
                        creatureTransform.rotation = Quaternion.LookRotation(directionToPlayer);
                    }
                }

                yield return null;
            }

            if (creatureAnimator != null)
            {
                creatureAnimator.SetFloat(speedFloatName, idleSpeedPercent);
            }

            LerpCreatureHeadAimWeight(0f);
            creatureActor.SetActive(false);
        }

        if (heartbeatAudioSource != null)
        {
            heartbeatAudioSource.Stop();
        }

        /*if (playerFeedbackController != null)
        {
            playerFeedbackController.StopPulse();
        }*/

        /*if (DialogueManager.Instance != null && policeCallReactionDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(policeCallReactionDialogue);

            while (DialogueManager.Instance.IsDialogueActive)
            {
                yield return null;
            }
        }*/

        if (wallPhone != null)
        {
            wallPhone.SetTelephoneUnlocked(true);
        }
    }
}