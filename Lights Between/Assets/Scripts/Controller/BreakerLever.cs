using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BreakerLever : MonoBehaviour, IInteractable
{
    [SerializeField] private ElectricBoxBreaker connectedBreaker;

    [SerializeField] private bool isLeverUp = true;
    [SerializeField] private bool isLeverMissing = false;
    [SerializeField] private float smoothSpeed = 1.5f;

    [SerializeField] private Transform leverHandleTransform;
    [SerializeField] private Collider leverCollider;
    [SerializeField] private GameObject leverWorldSpaceUI;

    [SerializeField] private Vector3 leverOffRotationEuler;
    [SerializeField] private Vector3 leverOnRotationEuler;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchOnSFX;
    [SerializeField] private AudioClip switchOffSFX;

    public event Action OnLeverStateChanged;

    public string promptMessage { get; private set; }
    public bool IsLeverUp => isLeverUp;
    public bool IsLeverMissing => isLeverMissing;

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotationCoroutine;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        offRotation = Quaternion.Euler(leverOffRotationEuler);
        onRotation = Quaternion.Euler(leverOnRotationEuler);

        if (leverHandleTransform != null)
        {
            leverHandleTransform.localRotation = isLeverUp ? onRotation : offRotation;
        }

        if (leverCollider == null && leverHandleTransform != null)
        {
            leverCollider = leverHandleTransform.GetComponent<Collider>();
        }

        UpdatePromptMessage();
    }

    public void Interact()
    {
        if (isLeverMissing) return;

        isLeverUp = !isLeverUp;
        UpdatePromptMessage();
        PlaySwitchSFX(isLeverUp);
        StartLeverAnimation();

        NotifyStateChanged();
    }

    public void SetLeverMissingState(bool missing)
    {
        isLeverMissing = missing;
        if (missing) isLeverUp = false;

        UpdatePromptMessage();

        NotifyStateChanged();
    }

    public void AttachMissingLever()
    {
        isLeverMissing = false;
        isLeverUp = true;

        UpdatePromptMessage();
        StartLeverAnimation();

        NotifyStateChanged();
    }

    public void TripLever()
    {
        if (!isLeverUp || isLeverMissing) return;

        isLeverUp = false;
        UpdatePromptMessage();
        PlaySwitchSFX(false);
        StartLeverAnimation();

        NotifyStateChanged();
    }

    private void PlaySwitchSFX(bool turningOn)
    {
        if (audioSource == null) return;

        AudioClip primarySFX = turningOn ? switchOnSFX : switchOffSFX;
        if (primarySFX != null)
        {
            audioSource.PlayOneShot(primarySFX);
        }
    }

    private void StartLeverAnimation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(AnimateLever());
    }

    private void UpdatePromptMessage()
    {
        if (isLeverMissing)
        {
            promptMessage = string.Empty;
            return;
        }

        promptMessage = isLeverUp ? "Pull down" : "Pull up";
    }

    private IEnumerator AnimateLever()
    {
        Quaternion targetRotation = isLeverUp ? onRotation : offRotation;

        while (leverHandleTransform != null && Quaternion.Angle(leverHandleTransform.localRotation, targetRotation) > 0.1f)
        {
            leverHandleTransform.localRotation = Quaternion.Slerp(
                leverHandleTransform.localRotation,
                targetRotation,
                Time.deltaTime * 8f * smoothSpeed
            );
            yield return null;
        }

        if (leverHandleTransform != null)
        {
            leverHandleTransform.localRotation = targetRotation;
        }
    }

    private void NotifyStateChanged()
    {
        OnLeverStateChanged?.Invoke();
        if (connectedBreaker != null)
        {
            connectedBreaker.RefreshPowerGrid();
        }
    }
}