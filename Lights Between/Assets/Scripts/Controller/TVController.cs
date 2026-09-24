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

    [Header("Indicator Light / Emission Settings")]
    [SerializeField] private Renderer tvLightRenderer;
    [SerializeField] private Color emissionOnColor = Color.red;
    [SerializeField] private Color emissionOffColor = Color.black;

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

    private void Awake()
    {
        emissionPropertyBlock = new MaterialPropertyBlock();

        if (connectedBreaker == null)
        {
            connectedBreaker = Object.FindFirstObjectByType<ElectricBoxBreaker>();
        }
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

        if (tvScreenRenderer != null && tvOffMaterial != null && tvOnMaterial != null)
        {
            Material[] mats = tvScreenRenderer.materials;
            if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
            {
                mats[screenMaterialIndex] = IsTVOn ? tvOnMaterial : tvOffMaterial;
                tvScreenRenderer.materials = mats;
            }
        }

        UpdateEmissionState(IsTVOn);

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

    private void UpdateEmissionState(bool isEmissiveOn)
    {
        if (tvLightRenderer == null) return;

        tvLightRenderer.GetPropertyBlock(emissionPropertyBlock);
        emissionPropertyBlock.SetColor(EmissionColorID, isEmissiveOn ? emissionOnColor : emissionOffColor);
        tvLightRenderer.SetPropertyBlock(emissionPropertyBlock);

        Material[] sharedMats = tvLightRenderer.sharedMaterials;
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
}