using UnityEngine;

public class TVController : MonoBehaviour, IInteractable
{
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

    private void Awake()
    {
        SetTVState(false);
    }

    public void Interact()
    {
        SetTVState(!IsTVOn);
    }

    public void SetTVState(bool turnOn)
    {
        IsTVOn = turnOn;

        // 1. Swap screen material
        if (tvScreenRenderer != null && tvOffMaterial != null && tvOnMaterial != null)
        {
            Material[] mats = tvScreenRenderer.materials;
            if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
            {
                mats[screenMaterialIndex] = IsTVOn ? tvOnMaterial : tvOffMaterial;
                tvScreenRenderer.materials = mats;
            }
        }

        // 2. Manage Audio
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

    public string GetPromptMessage()
    {
        return IsTVOn ? turnOffPrompt : turnOnPrompt;
    }
}