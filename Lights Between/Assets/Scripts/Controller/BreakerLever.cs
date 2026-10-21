using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BreakerLever : MonoBehaviour, IInteractable
{
    [SerializeField] private ElectricBoxBreaker connectedBreaker;

    [SerializeField] private bool isLeverUp = true;
    [SerializeField] private float smoothSpeed = 1.5f;

    [SerializeField] private Transform leverHandleTransform;
    [SerializeField] private Renderer[] leverHandleRenderers;
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

        if (leverHandleRenderers == null || leverHandleRenderers.Length == 0)
        {
            if (leverHandleTransform != null)
            {
                leverHandleRenderers = leverHandleTransform.GetComponentsInChildren<Renderer>(true);
            }
        }

        UpdateVisualsAndPrompts();
    }

    public void Interact()
    {
        if (connectedBreaker != null && connectedBreaker.IsLeverMissing) return;

        isLeverUp = !isLeverUp;
        UpdateVisualsAndPrompts();
        PlaySwitchSFX(isLeverUp);
        StartLeverAnimation();

        NotifyStateChanged();
    }

    public void SetLeverUpState(bool leverUp, bool animate = true)
    {
        isLeverUp = leverUp;
        UpdateVisualsAndPrompts();

        if (animate)
        {
            StartLeverAnimation();
        }
        else if (leverHandleTransform != null)
        {
            leverHandleTransform.localRotation = isLeverUp ? onRotation : offRotation;
        }

        NotifyStateChanged();
    }

    public void TripLever()
    {
        if (!isLeverUp || (connectedBreaker != null && connectedBreaker.IsLeverMissing)) return;

        isLeverUp = false;
        UpdateVisualsAndPrompts();
        PlaySwitchSFX(false);
        StartLeverAnimation();

        NotifyStateChanged();
    }

    public void UpdateVisualsAndPrompts()
    {
        bool isMissing = connectedBreaker != null && connectedBreaker.IsLeverMissing;
        bool showLeverVisuals = !isMissing;

        if (leverHandleRenderers != null)
        {
            for (int i = 0; i < leverHandleRenderers.Length; i++)
            {
                if (leverHandleRenderers[i] != null)
                {
                    leverHandleRenderers[i].enabled = showLeverVisuals;
                }
            }
        }

        if (leverWorldSpaceUI != null)
        {
            leverWorldSpaceUI.SetActive(showLeverVisuals);
        }

        if (leverCollider != null)
        {
            leverCollider.enabled = true;
        }

        if (isMissing)
        {
            promptMessage = string.Empty;
        }
        else
        {
            promptMessage = isLeverUp ? "Pull down" : "Pull up";
        }
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