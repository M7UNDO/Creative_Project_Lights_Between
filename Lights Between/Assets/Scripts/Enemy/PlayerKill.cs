using UnityEngine;
using UnityEngine.Rendering;

public class PlayerKill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator creatureAnimator;
    [SerializeField] private AudioSource jumpscareAudioSource;
    [SerializeField] private AudioClip jumpscareClip;
    [SerializeField] private ParticleSystem bloodParticles;
    [SerializeField] private Light faceSpotlight;
    [SerializeField] private Transform framingTarget;
    [SerializeField] private Volume jumpscareVolume;

    [Header("Dynamic Positioning Settings")]
    [SerializeField] private float spawnDistance = 1.5f;
    [SerializeField] private float heightOffset = -0.5f;
    [SerializeField] private float horizontalAngleOffset = 0f;

    private Transform targetCameraTransform;
    private FPSController targetPlayerController;
    private bool isTrackingTarget;

    private void LateUpdate()
    {
        if (!isTrackingTarget || targetPlayerController == null || framingTarget == null) return;

        targetPlayerController.ForceLookAt(framingTarget.position);
    }

    public void ExecuteCatchSequence(FPSController playerController, GameObject ambientCreature)
    {
        if (playerController == null) return;

        targetPlayerController = playerController;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            targetCameraTransform = mainCam.transform;
        }

        if (ambientCreature != null)
        {
            ambientCreature.SetActive(false);
        }

        targetPlayerController.SetJumpScareMode(true);

        PositionCreatureInitial();
        InitializeSequenceEffects();

        isTrackingTarget = true;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.TriggerPlayerDeath();
        }
    }

    public void OnCreatureScream(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.5f) return;

        AudioClip screamSound = animationEvent.objectReferenceParameter as AudioClip;
        if (jumpscareAudioSource != null && screamSound != null)
        {
            jumpscareAudioSource.PlayOneShot(screamSound);
        }
    }

    public void OnAttackImpact(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.5f) return;

        if (bloodParticles != null)
        {
            bloodParticles.Play();
        }

        AudioClip impactSound = animationEvent.objectReferenceParameter as AudioClip;
        if (jumpscareAudioSource != null && impactSound != null)
        {
            jumpscareAudioSource.PlayOneShot(impactSound);
        }
    }

    private void PositionCreatureInitial()
    {
        if (targetPlayerController == null) return;

        Transform playerTransform = targetPlayerController.transform;

        Vector3 spawnDirection = Quaternion.Euler(0f, horizontalAngleOffset, 0f) * playerTransform.forward;
        Vector3 finalPosition = playerTransform.position + (spawnDirection * spawnDistance);
        finalPosition.y += heightOffset;

        transform.position = finalPosition;

        Vector3 lookAtPlayerDir = playerTransform.position - transform.position;
        lookAtPlayerDir.y = 0f;
        if (lookAtPlayerDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookAtPlayerDir);
        }
    }

    private void InitializeSequenceEffects()
    {
        gameObject.SetActive(true);

        if (jumpscareVolume != null)
        {
            jumpscareVolume.gameObject.SetActive(true);
        }

        if (creatureAnimator != null)
        {
            creatureAnimator.SetTrigger("PlayJumpscare");
        }

        if (jumpscareAudioSource != null && jumpscareClip != null)
        {
            jumpscareAudioSource.PlayOneShot(jumpscareClip);
        }

        if (faceSpotlight != null)
        {
            faceSpotlight.enabled = true;
        }
    }
}