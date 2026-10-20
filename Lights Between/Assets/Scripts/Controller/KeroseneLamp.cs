using UnityEngine;
using UnityEngine.UI;

public class KeroseneLampScript : MonoBehaviour, IInteractable, IToolPower
{
    [Header("References")]
    [SerializeField] private HoldInteraction holdInteraction;
    [SerializeField] private GameObject lampLightObject;
    [SerializeField] private GameObject lampLightZone;
    [SerializeField] private GameObject flameObject;

    [Header("World Space UI Prompts")]
    [SerializeField] private WorldSpaceInteractableUI pickupUI;
    [SerializeField] private WorldSpaceInteractableUI igniteUI;

    [Header("Drain Settings")]
    [SerializeField] private float drainRate = 2f;
    [SerializeField] private float defaultMaxPower = 100f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject PowerDisplayObj;
    [SerializeField] private Image powerRadialFill;

    [Header("SFX")]
    [SerializeField] private AudioSource wickAudioSource;
    [SerializeField] private AudioClip sparkWickClip;

    [Header("Pickup Settings")]
    [SerializeField] private ToolClass lampToolData;

    private ToolClass toolData;
    private float currentPower;
    private float maxPower;
    private bool isEquipped;
    private bool isIgnited;

    private ToolEquipmentManager playerEquipmentManager;
    private PlayerToolInventory playerInventory;

    public float CurrentPower => currentPower;
    public float MaxPower => maxPower > 0f ? maxPower : defaultMaxPower;
    public bool UsesPower => toolData != null ? toolData.usesPower : true;
    public bool IsIgnited => isIgnited;

    private void Awake()
    {
        if (maxPower <= 0f)
        {
            maxPower = defaultMaxPower;
            currentPower = maxPower;
        }

        if (wickAudioSource != null && sparkWickClip != null)
        {
            wickAudioSource.clip = sparkWickClip;
        }

        if (flameObject != null) flameObject.SetActive(false);
        if (PowerDisplayObj != null) PowerDisplayObj.SetActive(false);
    }

    private void Start()
    {
        ResolvePlayerReferences();

        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.AddListener(Ignite);
        }

        UpdatePowerFill();
    }

    private void ResolvePlayerReferences()
    {
        if (playerInventory == null)
        {
            playerInventory = UnityEngine.Object.FindFirstObjectByType<PlayerToolInventory>();
        }

        if (playerEquipmentManager == null)
        {
            playerEquipmentManager = UnityEngine.Object.FindFirstObjectByType<ToolEquipmentManager>();
        }
    }

    public void UpdatePrompts(bool isFocused, bool playerHasLighter)
    {
        if (!isFocused)
        {
            if (pickupUI != null) pickupUI.HidePrompt();
            if (igniteUI != null) igniteUI.HidePrompt();
            return;
        }

        if (pickupUI != null)
        {
            pickupUI.ShowPrompt("[E] Pick up", true, true);
        }

        if (igniteUI != null)
        {
            if (!isIgnited && playerHasLighter && currentPower > 0f)
            {
                igniteUI.ShowPrompt("[Hold F] Ignite", true, true);
            }
            else
            {
                igniteUI.HidePrompt();
            }
        }
    }

    public void Interact()
    {
        PickupLamp();
    }

    public void TryIgnite()
    {
        if (isIgnited || currentPower <= 0f) return;

        ResolvePlayerReferences();

        if (playerInventory == null || !playerInventory.HasToolType(ToolType.Lighter))
        {
            return;
        }

        ToolClass lighterData = playerInventory.GetToolByType(ToolType.Lighter);

        if (playerEquipmentManager != null && lighterData != null && !playerEquipmentManager.IsEquipped(lighterData))
        {
            playerEquipmentManager.EquipTool(lighterData);
        }

        LighterScript currentLighter = playerEquipmentManager != null && playerEquipmentManager.CurrentToolObject != null
            ? playerEquipmentManager.CurrentToolObject.GetComponentInChildren<LighterScript>()
            : null;

        if (currentLighter == null || !currentLighter.IsLit)
        {
            return;
        }

        if (holdInteraction != null)
        {
            if (!holdInteraction.IsHolding)
            {
                holdInteraction.BeginHold();
            }
        }
        else
        {
            Ignite();
        }
    }

    public void CancelIgniteHold()
    {
        if (holdInteraction != null && holdInteraction.IsHolding)
        {
            holdInteraction.CancelHold();
        }
    }

    private void PickupLamp()
    {
        if (isEquipped) return;

        ResolvePlayerReferences();

        if (playerInventory != null && lampToolData != null)
        {
            playerInventory.AddTool(lampToolData);

            if (playerEquipmentManager != null && !playerEquipmentManager.HasToolEquipped)
            {
                playerEquipmentManager.EquipTool(lampToolData);
            }

            Destroy(gameObject);
        }
    }

    public void Initialise(ToolClass tool)
    {
        toolData = tool;
        maxPower = tool.maxPower;
        currentPower = maxPower;
        isEquipped = true;

        if (isIgnited)
        {
            HandlePower(true);
        }
    }

    public void SetPowerAndIgnitedState(float powerAmount, bool lit)
    {
        currentPower = Mathf.Clamp(powerAmount, 0f, MaxPower);
        isIgnited = lit && currentPower > 0f;

        HandlePower(isIgnited);
        UpdatePowerFill();
    }

    public void Ignite()
    {
        if (currentPower <= 0f) return;

        isIgnited = true;

        if (wickAudioSource != null)
        {
            wickAudioSource.Play();
        }
        HandlePower(isIgnited);
    }

    private void HandlePower(bool power)
    {
        if (lampLightObject != null) lampLightObject.SetActive(power);
        if (lampLightZone != null) lampLightZone.SetActive(power);
        if (flameObject != null) flameObject.SetActive(power);
    }

    public void DisplayPower()
    {
        if (isIgnited)
        {
            if (PowerDisplayObj != null) PowerDisplayObj.SetActive(true);
            UpdatePowerFill();
        }
    }

    public void HidePower()
    {
        if (PowerDisplayObj != null) PowerDisplayObj.SetActive(false);
    }

    private void UpdatePowerFill()
    {
        if (powerRadialFill != null)
        {
            float fillPercent = currentPower / MaxPower;
            powerRadialFill.fillAmount = fillPercent;
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