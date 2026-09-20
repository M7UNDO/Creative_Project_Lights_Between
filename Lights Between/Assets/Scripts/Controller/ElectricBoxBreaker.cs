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
    [SerializeField] private bool isLeverUp = true;
    [SerializeField] private float smoothSpeed = 1.5f;

    [Header("Lever Handle Animation")]
    [SerializeField] private Transform leverHandleTransform;
    [SerializeField] private Vector3 leverOffRotationEuler;
    [SerializeField] private Vector3 leverOnRotationEuler;

    [Header("Breaker Status Indicator Lights")]
    [SerializeField] private Renderer redStatusRenderer;
    [SerializeField] private Renderer greenStatusRenderer;
    [SerializeField] private Color redIndicatorColor = Color.red;
    [SerializeField] private Color greenIndicatorColor = Color.green;

    [Header("Connected Power Grid Outputs")]
    [SerializeField] private Light[] connectedLights;
    [SerializeField] private GameObject[] connectedLightObjects;

    [Header("Material Swapping Renderers")]
    [SerializeField] private Renderer[] swappableLightRenderers;
    [SerializeField] private Material lightOffMaterial;
    [SerializeField] private Material lightOnMaterial;

    [Header("Emission Toggling Renderers")]
    [SerializeField] private Renderer[] emissiveLightRenderers;
    [SerializeField] private Color emissionOnColor = Color.white;
    [SerializeField] private Color emissionOffColor = Color.black;

    [Header("Baked Lightmaps")]
    [SerializeField] private Texture2D[] darkLightmapDir;
    [SerializeField] private Texture2D[] darkLightmapColour;
    [SerializeField] private Texture2D[] brightLightmapDir;
    [SerializeField] private Texture2D[] brightLightmapColour;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchOnSFX;
    [SerializeField] private AudioClip switchOffSFX;
    [SerializeField] private AudioClip powerOutageSFX;

    [Header("Events")]
    public UnityEvent onPowerRestored;
    public UnityEvent onPowerSevered;

    public event Action<bool> OnPowerGridStateChanged;

    public string promptMessage { get; private set; }

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotationCoroutine;

    private LightmapData[] darkLightmaps;
    private LightmapData[] brightLightmaps;

    private MaterialPropertyBlock emissionPropertyBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private bool previousGridState;

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

        emissionPropertyBlock = new MaterialPropertyBlock();

        BuildLightmapDataCache();
        UpdatePromptMessage();
    }

    private void Start()
    {
        if (connectedGenerator != null)
        {
            connectedGenerator.onGeneratorStarted.AddListener(RefreshPowerGrid);
            connectedGenerator.onGeneratorStopped.AddListener(RefreshPowerGrid);
        }

        previousGridState = IsGridActive;
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
            AudioClip primarySFX = isLeverUp ? switchOnSFX : switchOffSFX;
            if (primarySFX != null) audioSource.PlayOneShot(primarySFX);
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
            audioSource.PlayOneShot(switchOffSFX);
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

        SwitchLightmaps(targetState);
        UpdateLightsState(targetState);
        UpdateSwappedMaterials(targetState);
        UpdateEmissiveMaterials(targetState);
        UpdateStatusIndicators();

        if (targetState)
        {
            onPowerRestored?.Invoke();
        }
        else
        {
            if (previousGridState && audioSource != null && powerOutageSFX != null)
            {
                audioSource.PlayOneShot(powerOutageSFX);
            }

            onPowerSevered?.Invoke();
        }

        previousGridState = targetState;
    }

    private void UpdateStatusIndicators()
    {
        bool hasGeneratorPower = connectedGenerator != null && connectedGenerator.CurrentPower > 0f;

        // Green Indicator: ON when generator has power, OFF when no power
        SetRendererEmission(greenStatusRenderer, hasGeneratorPower, greenIndicatorColor);

        // Red Indicator: ON when generator has NO power, OFF when power is supplied
        SetRendererEmission(redStatusRenderer, !hasGeneratorPower, redIndicatorColor);
    }

    private void SetRendererEmission(Renderer rendererTarget, bool isEmissiveOn, Color emissiveColor)
    {
        if (rendererTarget == null) return;

        rendererTarget.GetPropertyBlock(emissionPropertyBlock);
        emissionPropertyBlock.SetColor(EmissionColorID, isEmissiveOn ? emissiveColor : Color.black);
        rendererTarget.SetPropertyBlock(emissionPropertyBlock);

        Material[] sharedMats = rendererTarget.sharedMaterials;
        if (sharedMats == null) return;

        for (int i = 0; i < sharedMats.Length; i++)
        {
            if (sharedMats[i] == null) continue;

            if (isEmissiveOn)
            {
                sharedMats[i].EnableKeyword("_EMISSION");
            }
            else
            {
                sharedMats[i].DisableKeyword("_EMISSION");
            }
        }
    }

    private void BuildLightmapDataCache()
    {
        if (darkLightmapColour != null)
        {
            int darkCount = darkLightmapColour.Length;
            darkLightmaps = new LightmapData[darkCount];
            for (int i = 0; i < darkCount; i++)
            {
                darkLightmaps[i] = new LightmapData
                {
                    lightmapColor = darkLightmapColour[i],
                    lightmapDir = (darkLightmapDir != null && i < darkLightmapDir.Length) ? darkLightmapDir[i] : null
                };
            }
        }

        if (brightLightmapColour != null)
        {
            int brightCount = brightLightmapColour.Length;
            brightLightmaps = new LightmapData[brightCount];
            for (int i = 0; i < brightCount; i++)
            {
                brightLightmaps[i] = new LightmapData
                {
                    lightmapColor = brightLightmapColour[i],
                    lightmapDir = (brightLightmapDir != null && i < brightLightmapDir.Length) ? brightLightmapDir[i] : null
                };
            }
        }
    }

    private void SwitchLightmaps(bool isPowered)
    {
        LightmapData[] targetLightmaps = isPowered ? brightLightmaps : darkLightmaps;

        if (targetLightmaps != null && targetLightmaps.Length > 0)
        {
            LightmapSettings.lightmaps = targetLightmaps;
        }
    }

    private void UpdateLightsState(bool targetState)
    {
        if (connectedLights != null)
        {
            for (int i = 0; i < connectedLights.Length; i++)
            {
                if (connectedLights[i] != null) connectedLights[i].enabled = targetState;
            }
        }

        if (connectedLightObjects != null)
        {
            for (int i = 0; i < connectedLightObjects.Length; i++)
            {
                if (connectedLightObjects[i] != null) connectedLightObjects[i].SetActive(targetState);
            }
        }
    }

    private void UpdateSwappedMaterials(bool lightsAreOn)
    {
        if (swappableLightRenderers == null || lightOffMaterial == null || lightOnMaterial == null) return;

        for (int i = 0; i < swappableLightRenderers.Length; i++)
        {
            Renderer lightRenderer = swappableLightRenderers[i];
            if (lightRenderer == null) continue;

            Material[] sharedMats = lightRenderer.sharedMaterials;
            if (sharedMats == null || sharedMats.Length == 0) continue;

            bool materialModified = false;
            for (int j = 0; j < sharedMats.Length; j++)
            {
                if (sharedMats[j] == null) continue;

                if (lightsAreOn && sharedMats[j] == lightOffMaterial)
                {
                    sharedMats[j] = lightOnMaterial;
                    materialModified = true;
                }
                else if (!lightsAreOn && sharedMats[j] == lightOnMaterial)
                {
                    sharedMats[j] = lightOffMaterial;
                    materialModified = true;
                }
            }

            if (materialModified)
            {
                lightRenderer.sharedMaterials = sharedMats;
            }
        }
    }

    private void UpdateEmissiveMaterials(bool lightsAreOn)
    {
        if (emissiveLightRenderers == null) return;

        for (int i = 0; i < emissiveLightRenderers.Length; i++)
        {
            SetRendererEmission(emissiveLightRenderers[i], lightsAreOn, emissionOnColor);
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