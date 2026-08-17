using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class ElectricBoxBreaker : MonoBehaviour, IInteractable
{
    [Header("Power Input Connection")]
    [SerializeField] private GeneratorPowerSystem connectedGenerator;

    [Header("Breaker State")]
    [SerializeField] private bool isLeverUp = false;
    [SerializeField] private float smoothSpeed = 1.5f;

    [Header("Lever Handle Animation")]
    [SerializeField] private Transform leverHandleTransform;
    [SerializeField] private Vector3 leverOffRotationEuler;
    [SerializeField] private Vector3 leverOnRotationEuler;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchOnSFX;
    [SerializeField] private AudioClip switchOffSFX;

    [Header("Events")]
    public UnityEvent onPowerRestored;
    public UnityEvent onPowerSevered;

    public event Action<bool> OnPowerGridStateChanged;

    public string promptMessage { get; private set; }

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotationCoroutine;

    private readonly List<LightSwitch> registeredSwitches = new List<LightSwitch>();

    public bool IsGridActive => isLeverUp && connectedGenerator != null && connectedGenerator.CurrentPower > 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        offRotation = Quaternion.Euler(leverOffRotationEuler);
        onRotation = Quaternion.Euler(leverOnRotationEuler);

        if (leverHandleTransform != null)
        {
            leverHandleTransform.localRotation = isLeverUp ? onRotation : offRotation;
        }

        UpdatePromptMessage();
    }

    private void Start()
    {
        if (connectedGenerator != null)
        {
            connectedGenerator.onGeneratorStarted.AddListener(RefreshPowerGrid);
            connectedGenerator.onGeneratorStopped.AddListener(RefreshPowerGrid);
        }

        RefreshPowerGrid();
    }

    private void OnDestroy()
    {
        if (connectedGenerator != null)
        {
            connectedGenerator.onGeneratorStarted.RemoveListener(RefreshPowerGrid);
            connectedGenerator.onGeneratorStopped.RemoveListener(RefreshPowerGrid);
        }
    }

    public void RegisterLightSwitch(LightSwitch lightSwitch)
    {
        if (lightSwitch != null && !registeredSwitches.Contains(lightSwitch))
        {
            registeredSwitches.Add(lightSwitch);
        }
    }

    public void UnregisterLightSwitch(LightSwitch lightSwitch)
    {
        if (lightSwitch != null && registeredSwitches.Contains(lightSwitch))
        {
            registeredSwitches.Remove(lightSwitch);
        }
    }

    public float GetTotalActiveLightDrain()
    {
        if (!IsGridActive) return 0f;

        float totalDrain = 0f;
        foreach (LightSwitch lightSwitch in registeredSwitches)
        {
            if (lightSwitch != null)
            {
                totalDrain += lightSwitch.CurrentPowerDrain;
            }
        }

        return totalDrain;
    }

    public void Interact()
    {
        isLeverUp = !isLeverUp;
        UpdatePromptMessage();

        if (audioSource != null)
        {
            audioSource.clip = isLeverUp ? switchOnSFX : switchOffSFX;
            audioSource.Play();
        }

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(AnimateLever());

        RefreshPowerGrid();
    }

    public void TripBreaker()
    {
        if (!isLeverUp) return;

        isLeverUp = false;
        UpdatePromptMessage();

        if (audioSource != null && switchOffSFX != null)
        {
            audioSource.clip = switchOffSFX;
            audioSource.Play();
        }

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(AnimateLever());

        RefreshPowerGrid();
    }

    public void RefreshPowerGrid()
    {
        bool targetState = IsGridActive;

        OnPowerGridStateChanged?.Invoke(targetState);

        if (targetState)
        {
            onPowerRestored?.Invoke();
        }
        else
        {
            onPowerSevered?.Invoke();
        }
    }

    private void UpdatePromptMessage()
    {
        promptMessage = isLeverUp ? "Pull down" : "Pull up";
    }

    private IEnumerator AnimateLever()
    {
        Quaternion targetRotation = isLeverUp ? onRotation : offRotation;

        while (Quaternion.Angle(leverHandleTransform.localRotation, targetRotation) > 0.1f)
        {
            leverHandleTransform.localRotation = Quaternion.Slerp(
                leverHandleTransform.localRotation,
                targetRotation,
                Time.deltaTime * 8f * smoothSpeed
            );
            yield return null;
        }

        leverHandleTransform.localRotation = targetRotation;
    }
}