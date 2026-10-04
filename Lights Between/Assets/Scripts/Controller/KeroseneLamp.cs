using UnityEngine;
using UnityEngine.UI;

public class KeroseneLampScript : MonoBehaviour, IInteractable, IToolPower
{
    [Header("References")]
    [SerializeField] private HoldInteraction holdInteraction;
    [SerializeField] private GameObject lampLightObject;
    [SerializeField] private GameObject lampLightZone;
    [SerializeField] private GameObject flameObject;

    [Header("Drain Settings")]
    [SerializeField] private float drainRate = 2f;
    [SerializeField] private float defaultMaxPower = 100f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject PowerDisplayObj;
    [SerializeField] private Image powerRadialFill;

    [Header("SFX")]
    [SerializeField] private AudioSource wickAudioSource;
    [SerializeField] private AudioClip sparkWickClip;

    private ToolClass toolData;
    private float currentPower;
    private float maxPower;
    private bool isEquipped;

    public float CurrentPower => currentPower;
    public float MaxPower => maxPower > 0f ? maxPower : defaultMaxPower;
    public bool UsesPower => toolData != null ? toolData.usesPower : true;

    private bool isIgnited;

    private ToolEquipmentManager playerEquipmentManager;
    private PlayerToolInventory playerInventory;

    private void Awake()
    {
        if (maxPower <= 0f)
        {
            maxPower = defaultMaxPower;
            currentPower = maxPower;
        }

        if(wickAudioSource && sparkWickClip != null)
        {
            wickAudioSource.clip = sparkWickClip;
        }

        if (flameObject != null)
        {
            flameObject.SetActive(false);
        }

        if (PowerDisplayObj != null)
        {
            PowerDisplayObj.SetActive(false);
        }
    }

    private void Start()
    {
        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.AddListener(Ignite);
        }

        UpdatePowerFill();
    }

    public void Interact()
    {
        if (holdInteraction == null) return;

        holdInteraction.BeginHold();
    }

    public void CancelInteract()
    {
        if (holdInteraction != null && holdInteraction.IsHolding)
        {
            holdInteraction.CancelHold();
        }
    }

    public void DisplayPower()
    {
        if (isIgnited)
        {
            if (PowerDisplayObj != null)
            {
                PowerDisplayObj.SetActive(true);
            }
            UpdatePowerFill();
        }
    }

    public void HidePower()
    {
        if (PowerDisplayObj != null)
        {
            PowerDisplayObj.SetActive(false);
        }
    }

    private void UpdatePowerFill()
    {
        if (powerRadialFill != null)
        {
            float fillPercent = currentPower / maxPower;
            powerRadialFill.fillAmount = fillPercent;
        }
    }

    public void Initialise(ToolClass tool)
    {
        toolData = tool;
        maxPower = tool.maxPower;
        currentPower = maxPower;
        isEquipped = true;

        if (lampLightObject != null)
        {
            lampLightObject.SetActive(true);
        }

        if (lampLightZone != null)
        {
            lampLightZone.SetActive(true);
        }
    }

    public void Ignite()
    {
        isIgnited = true;

        if(wickAudioSource != null)
        {
            wickAudioSource.Play();
        }
        HandlePower(isIgnited);
    }

    private void HandlePower(bool power)
    {
        if (lampLightObject != null)
        {
            lampLightObject.SetActive(power);
        }

        if (lampLightZone != null)
        {
            lampLightZone.SetActive(power);
        }

        if (flameObject != null)
        {
            flameObject.SetActive(power);
        }
    }

    private void Update()
    {
        if (toolData != null && !isEquipped) return;
        if (currentPower <= 0f) return;

        if (isIgnited)
        {
            currentPower -= drainRate * Time.deltaTime;
            currentPower = Mathf.Clamp(currentPower, 0f, maxPower);

            UpdatePowerFill();

            if (currentPower <= 0f)
            {
                isIgnited = false;
                HandlePower(false);
            }
        }
    }
}