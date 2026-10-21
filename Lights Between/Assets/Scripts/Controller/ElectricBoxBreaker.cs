using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class ElectricBoxBreaker : MonoBehaviour
{
    [SerializeField] private BreakerLever connectedLever;

    [SerializeField] private GeneratorPowerSystem connectedGenerator;

    [SerializeField] private bool isLeverMissing = false;
    [SerializeField] private GameObject snapZoneObject;

    [SerializeField] private Renderer redStatusRenderer;
    [SerializeField] private Renderer greenStatusRenderer;
    [SerializeField] private Color redIndicatorColor = Color.red;
    [SerializeField] private Color greenIndicatorColor = Color.green;

    [SerializeField] private Light[] connectedLights;
    [SerializeField] private GameObject[] connectedLightObjects;

    [SerializeField] private Renderer[] swappableLightRenderers;
    [SerializeField] private Material lightOffMaterial;
    [SerializeField] private Material lightOnMaterial;

    [SerializeField] private Renderer[] emissiveLightRenderers;
    [SerializeField] private Color emissionOnColor = Color.white;
    [SerializeField] private Color emissionOffColor = Color.black;

    [SerializeField] private Texture2D[] darkLightmapDir;
    [SerializeField] private Texture2D[] darkLightmapColour;
    [SerializeField] private Texture2D[] brightLightmapDir;
    [SerializeField] private Texture2D[] brightLightmapColour;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip powerOutageSFX;

    public UnityEvent onPowerRestored;
    public UnityEvent onPowerSevered;

    public event Action<bool> OnPowerGridStateChanged;

    private LightmapData[] darkLightmaps;
    private LightmapData[] brightLightmaps;

    private MaterialPropertyBlock emissionPropertyBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private bool previousGridState;

    public bool IsLeverMissing => isLeverMissing;

    public bool IsGridActive => !isLeverMissing
        && connectedLever != null
        && connectedLever.IsLeverUp
        && connectedGenerator != null
        && connectedGenerator.CurrentPower > 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        emissionPropertyBlock = new MaterialPropertyBlock();

        BuildLightmapDataCache();

        if (snapZoneObject != null)
        {
            snapZoneObject.SetActive(isLeverMissing);
        }
    }

    private void OnEnable()
    {
        if (connectedLever != null)
        {
            connectedLever.OnLeverStateChanged += RefreshPowerGrid;
        }

        if (connectedGenerator != null)
        {
            connectedGenerator.onGeneratorStarted.AddListener(RefreshPowerGrid);
            connectedGenerator.onGeneratorStopped.AddListener(RefreshPowerGrid);
        }
    }

    private void OnDisable()
    {
        if (connectedLever != null)
        {
            connectedLever.OnLeverStateChanged -= RefreshPowerGrid;
        }

        if (connectedGenerator != null)
        {
            connectedGenerator.onGeneratorStarted.RemoveListener(RefreshPowerGrid);
            connectedGenerator.onGeneratorStopped.RemoveListener(RefreshPowerGrid);
        }
    }

    private void Start()
    {
        if (connectedLever != null)
        {
            connectedLever.UpdateVisualsAndPrompts();
        }

        previousGridState = IsGridActive;
        RefreshPowerGrid();
    }

    public void SetLeverMissingState(bool missing)
    {
        isLeverMissing = missing;

        if (snapZoneObject != null)
        {
            snapZoneObject.SetActive(isLeverMissing);
        }

        if (connectedLever != null)
        {
            if (missing)
            {
                connectedLever.SetLeverUpState(false, false);
            }
            else
            {
                connectedLever.UpdateVisualsAndPrompts();
            }
        }

        RefreshPowerGrid();
    }

    public void AttachMissingLever()
    {
        isLeverMissing = false;

        if (snapZoneObject != null)
        {
            snapZoneObject.SetActive(false);
        }

        if (connectedLever != null)
        {
            connectedLever.SetLeverUpState(true, true);
        }

        RefreshPowerGrid();
    }

    public void TripBreaker()
    {
        if (connectedLever != null)
        {
            connectedLever.TripLever();
        }
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

        SetRendererEmission(greenStatusRenderer, hasGeneratorPower, greenIndicatorColor);
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
}