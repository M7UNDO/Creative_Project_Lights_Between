using System.Collections;
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Power Dependencies")]
    [SerializeField] private ElectricBoxBreaker electricBoxBreaker;

    [Header("Power Consumption")]
    [SerializeField] private float drainPerLight = 0.05f;

    [Header("Switch Visual State")]
    [SerializeField] private bool isSwitchOn = false;
    [SerializeField] private Transform switchHandleTransform;
    [SerializeField] private Vector3 switchOffRotationEuler;
    [SerializeField] private Vector3 switchOnRotationEuler;
    [SerializeField] private float smoothSpeed = 1.5f;

    [Header("Controlled Light Outputs")]
    [SerializeField] private Light[] connectedLights;
    [SerializeField] private GameObject[] connectedLightObjects;

    [Header("Light Fixture Materials")]
    [SerializeField] private Renderer[] lightRenderers;
    [SerializeField] private Material lightOffMaterial;
    [SerializeField] private Material lightOnMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchOnSFX;
    [SerializeField] private AudioClip switchOffSFX;

    public string promptMessage { get; private set; }

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotationCoroutine;

    public bool IsLightActive => isSwitchOn && (electricBoxBreaker != null && electricBoxBreaker.IsGridActive);

    public float CurrentPowerDrain
    {
        get
        {
            if (!IsLightActive) return 0f;

            int activeLightCount = (connectedLights != null && connectedLights.Length > 0)
                ? connectedLights.Length
                : 1;

            return activeLightCount * drainPerLight;
        }
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        offRotation = Quaternion.Euler(switchOffRotationEuler);
        onRotation = Quaternion.Euler(switchOnRotationEuler);

        if (switchHandleTransform != null)
        {
            switchHandleTransform.localRotation = isSwitchOn ? onRotation : offRotation;
        }

        if (electricBoxBreaker == null)
        {
            electricBoxBreaker = Object.FindFirstObjectByType<ElectricBoxBreaker>();
        }

        UpdatePromptMessage();
    }

    private void OnEnable()
    {
        if (electricBoxBreaker != null)
        {
            electricBoxBreaker.RegisterLightSwitch(this);
            electricBoxBreaker.OnPowerGridStateChanged += HandlePowerGridStateChanged;
        }
    }

    private void OnDisable()
    {
        if (electricBoxBreaker != null)
        {
            electricBoxBreaker.UnregisterLightSwitch(this);
            electricBoxBreaker.OnPowerGridStateChanged -= HandlePowerGridStateChanged;
        }
    }

    private void Start()
    {
        RefreshSwitchState();
    }

    public void Interact()
    {
        ToggleSwitch();
    }

    public void ToggleSwitch()
    {
        isSwitchOn = !isSwitchOn;
        UpdatePromptMessage();

        PlaySFX(isSwitchOn ? switchOnSFX : switchOffSFX);

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(AnimateSwitch());

        RefreshSwitchState();
    }

    private void HandlePowerGridStateChanged(bool isGridActive)
    {
        RefreshSwitchState();
    }

    public void RefreshSwitchState()
    {
        bool state = IsLightActive;

        if (connectedLights != null)
        {
            foreach (Light lightComponent in connectedLights)
            {
                if (lightComponent != null) lightComponent.enabled = state;
            }
        }

        if (connectedLightObjects != null)
        {
            foreach (GameObject lightObj in connectedLightObjects)
            {
                if (lightObj != null) lightObj.SetActive(state);
            }
        }

        UpdateLightMaterials(state);
    }

    private void UpdateLightMaterials(bool activeState)
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
                if (activeState && matName == lightOffMaterial.name)
                {
                    mats[i] = lightOnMaterial;
                }
                else if (!activeState && matName == lightOnMaterial.name)
                {
                    mats[i] = lightOffMaterial;
                }
            }

            lightRenderer.materials = mats;
        }
    }

    private void UpdatePromptMessage()
    {
        promptMessage = isSwitchOn ? "Turn Off" : "Turn On";
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator AnimateSwitch()
    {
        if (switchHandleTransform == null) yield break;

        Quaternion targetRotation = isSwitchOn ? onRotation : offRotation;

        while (Quaternion.Angle(switchHandleTransform.localRotation, targetRotation) > 0.1f)
        {
            switchHandleTransform.localRotation = Quaternion.Slerp(
                switchHandleTransform.localRotation,
                targetRotation,
                Time.deltaTime * 8f * smoothSpeed
            );
            yield return null;
        }

        switchHandleTransform.localRotation = targetRotation;
    }
}