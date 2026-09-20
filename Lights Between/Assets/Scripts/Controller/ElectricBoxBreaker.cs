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

    [Header("Connected Power Grid Outputs")]
    [SerializeField] private Light[] connectedLights;
    [SerializeField] private GameObject[] connectedLightObjects;

    [Header("Light Materials")]
    [SerializeField] private Renderer[] lightRenderers;
    [SerializeField] private Material lightOffMaterial;
    [SerializeField] private Material lightOnMaterial;

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

        if (connectedLights != null)
        {
            foreach (Light light in connectedLights)
            {
                if (light != null) light.enabled = targetState;
            }
        }

        if (connectedLightObjects != null)
        {
            foreach (GameObject lightObj in connectedLightObjects)
            {
                if (lightObj != null) lightObj.SetActive(targetState);
            }
        }

        UpdateLightMaterials(targetState);

        if (targetState)
        {
            onPowerRestored?.Invoke();
        }
        else
        {
            onPowerSevered?.Invoke();
        }
    }

    private void UpdateLightMaterials(bool lightsAreOn)
    {
        if (lightRenderers == null || lightOffMaterial == null || lightOnMaterial == null) return;

        foreach (Renderer lightRenderer in lightRenderers)
        {
            if (lightRenderer == null) continue;

            Material[] mats = lightRenderer.materials;
            if (mats == null || mats.Length == 0) continue;

            for (int i = 0; i < mats.Length; i++)
            {
                string matName = mats[i].name.Replace(" (Instance)", "");
                if (lightsAreOn && matName == lightOffMaterial.name)
                {
                    mats[i] = lightOnMaterial;
                }
                else if (!lightsAreOn && matName == lightOnMaterial.name)
                {
                    mats[i] = lightOffMaterial;
                }
            }

            lightRenderer.materials = mats;
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