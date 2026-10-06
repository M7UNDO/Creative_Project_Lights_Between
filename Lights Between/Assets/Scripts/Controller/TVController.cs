using UnityEngine;

public class TVController : MonoBehaviour, IInteractable
{
    [Header("Power Connection")]
    [SerializeField] private ElectricBoxBreaker connectedBreaker;

    [Header("Screen References")]
    [SerializeField] private MeshRenderer tvScreenRenderer;
    [SerializeField] private int screenMaterialIndex = 0;
    [SerializeField] private Material tvOffMaterial;
    [SerializeField] private Material tvOnMaterial;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource tvAudioSource;
    [SerializeField] private AudioClip tvOnSFX;
    [SerializeField] private AudioClip tvOffSFX;
    [SerializeField] private AudioClip tvStaticLoopSFX;

    [Header("Prompts")]
    [SerializeField] private string turnOnPrompt = "Turn On TV";
    [SerializeField] private string turnOffPrompt = "Turn Off TV";

    public bool IsTVOn { get; private set; }

    private MaterialPropertyBlock emissionPropertyBlock;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private Material[] sharedMaterialsCache;

    private void Awake()
    {
        emissionPropertyBlock = new MaterialPropertyBlock();

        if (connectedBreaker == null)
        {
            connectedBreaker = Object.FindFirstObjectByType<ElectricBoxBreaker>();
        }

        if (tvScreenRenderer != null)
        {
            sharedMaterialsCache = tvScreenRenderer.sharedMaterials;
        }

        PreloadAudioClips();
    }

    private void OnEnable()
    {
        if (connectedBreaker != null)
        {
            connectedBreaker.OnPowerGridStateChanged += HandlePowerGridStateChanged;
        }
    }

    private void OnDisable()
    {
        if (connectedBreaker != null)
        {
            connectedBreaker.OnPowerGridStateChanged -= HandlePowerGridStateChanged;
        }
    }

    public void Interact()
    {
        if (connectedBreaker != null && !connectedBreaker.IsGridActive)
        {
            return;
        }

        SetTVState(!IsTVOn);
    }

    public void SetTVState(bool turnOn)
    {
        IsTVOn = turnOn;

        if (tvScreenRenderer != null && tvOffMaterial != null && tvOnMaterial != null && sharedMaterialsCache != null)
        {
            if (screenMaterialIndex >= 0 && screenMaterialIndex < sharedMaterialsCache.Length)
            {
                sharedMaterialsCache[screenMaterialIndex] = IsTVOn ? tvOnMaterial : tvOffMaterial;
                tvScreenRenderer.sharedMaterials = sharedMaterialsCache;
            }
        }

        if (tvAudioSource != null)
        {
            tvAudioSource.Stop();

            AudioClip toggleClip = IsTVOn ? tvOnSFX : tvOffSFX;
            if (toggleClip != null)
            {
                tvAudioSource.PlayOneShot(toggleClip);
            }

            if (IsTVOn && tvStaticLoopSFX != null)
            {
                tvAudioSource.clip = tvStaticLoopSFX;
                tvAudioSource.loop = true;
                tvAudioSource.Play();
            }
        }
    }

    private void HandlePowerGridStateChanged(bool isPowerActive)
    {
        if (!isPowerActive && IsTVOn)
        {
            SetTVState(false);
        }
    }

    public string GetPromptMessage()
    {
        return IsTVOn ? turnOffPrompt : turnOnPrompt;
    }

    private void PreloadAudioClips()
    {
        if (tvOnSFX != null && tvOnSFX.loadState != AudioDataLoadState.Loaded)
        {
            tvOnSFX.LoadAudioData();
        }
        if (tvOffSFX != null && tvOffSFX.loadState != AudioDataLoadState.Loaded)
        {
            tvOffSFX.LoadAudioData();
        }
        if (tvStaticLoopSFX != null && tvStaticLoopSFX.loadState != AudioDataLoadState.Loaded)
        {
            tvStaticLoopSFX.LoadAudioData();
        }
    }
}