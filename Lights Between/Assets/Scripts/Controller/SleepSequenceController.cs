using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SleepSequenceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPoseAnchor bedPoseAnchor;
    [SerializeField] private GeneratorPowerSystem targetGenerator;
    [SerializeField] private Volume postProcessingVolume;

    [Header("Objects to Deactivate on Interaction")]
    [SerializeField] private List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Header("Sequence Timings")]
    [SerializeField] private float sleepDelay = 1.5f;
    [SerializeField] private float eyelidCloseDuration = 3.0f;
    [SerializeField] private float blackoutDuration = 2.0f;

    [Header("Objectives to Progress")]
    [SerializeField] private ObjectiveSO sleepObjectiveToComplete;
    [SerializeField] private ObjectiveSO findTorchObjectiveToActivate;

    [Header("Dialogue Triggers")]
    [SerializeField] private DialogueSO powerOutageReactionDialogue;

    private Vignette vignetteComponent;

    private void Awake()
    {
        if (postProcessingVolume != null && postProcessingVolume.profile != null)
        {
            postProcessingVolume.profile.TryGet(out vignetteComponent);
        }
    }

    private void OnEnable()
    {
        if (bedPoseAnchor != null)
        {
            bedPoseAnchor.OnPoseEntered += OnPlayerEnteredBed;
        }
    }

    private void OnDisable()
    {
        if (bedPoseAnchor != null)
        {
            bedPoseAnchor.OnPoseEntered -= OnPlayerEnteredBed;
        }
    }

    private void OnPlayerEnteredBed()
    {
        DeactivateTargetObjects();
        StartCoroutine(PlaySleepAndPowerOutageSequence());
    }

    private IEnumerator PlaySleepAndPowerOutageSequence()
    {
        float originalVignette = 0.4f;

        if (bedPoseAnchor != null)
        {
            bedPoseAnchor.SetStandUpPermission(false);
        }

        if (vignetteComponent != null)
        {
            originalVignette = vignetteComponent.intensity.value;
        }

        yield return new WaitForSeconds(sleepDelay);

        if (vignetteComponent != null)
        {
            vignetteComponent.active = true;
            float elapsed = 0f;

            while (elapsed < eyelidCloseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / eyelidCloseDuration;
                vignetteComponent.intensity.value = Mathf.Lerp(originalVignette, 1.0f, t * t);
                yield return null;
            }
            vignetteComponent.intensity.value = 1.0f;
        }

        if (ObjectiveManager.Instance != null && sleepObjectiveToComplete != null)
        {
            ObjectiveManager.Instance.CompleteObjective(sleepObjectiveToComplete.ID);
        }

        yield return new WaitForSeconds(blackoutDuration);

        if (targetGenerator != null)
        {
            targetGenerator.ForceOutageNoFuel();
        }

        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.TransitionToState(MusicState.Tension, 0.5f);
        }

        if (vignetteComponent != null)
        {
            float elapsed = 0f;
            while (elapsed < 1.0f)
            {
                elapsed += Time.deltaTime * 2f;
                vignetteComponent.intensity.value = Mathf.Lerp(1.0f, originalVignette, elapsed);
                yield return null;
            }
            vignetteComponent.intensity.value = originalVignette;
        }

        if (DialogueManager.Instance != null && powerOutageReactionDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(powerOutageReactionDialogue);
        }

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return null;
        }

        if (bedPoseAnchor != null)
        {
            bedPoseAnchor.SetStandUpPermission(true);
        }

        if (ObjectiveManager.Instance != null && findTorchObjectiveToActivate != null)
        {
            ObjectiveManager.Instance.ActivateObjective(findTorchObjectiveToActivate);
        }
    }

    private void DeactivateTargetObjects()
    {
        if (objectsToDeactivate == null) return;

        for (int i = 0; i < objectsToDeactivate.Count; i++)
        {
            if (objectsToDeactivate[i] != null)
            {
                objectsToDeactivate[i].SetActive(false);
            }
        }
    }
}