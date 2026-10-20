using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightScript : MonoBehaviour, IToolPower
{
    [Header("References", order = 0)]
    [Space(5)]
    [SerializeField] private GameObject TorchLight;
    [SerializeField] private FPSController fpsController;
    [SerializeField] private HoldInteraction holdInteraction;
    [SerializeField] private Animator animator;

    [Header("Inventory Settings", order = 1)]
    [Space(5)]
    [SerializeField] private ItemClass batteryItemData;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Visuals", order = 2)]
    [Space(5)]
    [SerializeField] private Texture lightCookie;
    [SerializeField] private Renderer glassRenderer;

    [Header("SFX", order = 3)]
    [Space(5)]
    [SerializeField] private AudioSource flashlightAudioSource;
    [SerializeField] private AudioClip torchClickClip;
    [SerializeField] private AudioClip batteryChangeClip;
    [SerializeField] private AudioClip batteryEmptyClip;

    [Header("Lag & Tracking", order = 4)]
    [Space(5)]
    [SerializeField] private float positionLagSpeed = 12f;
    [SerializeField] private float rotationLagSpeed = 8f;

    [Header("Mouse/Stick Sway Settings", order = 5)]
    [Space(5)]
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float maxSwayAmount = 4f;
    [SerializeField] private float swaySmoothness = 6f;

    [Header("Movement Bobbing Settings", order = 6)]
    [Space(5)]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmountX = 0.025f;
    [SerializeField] private float walkBobAmountY = 0.025f;

    [Header("Drain Settings", order = 7)]
    [Space(5)]
    [SerializeField] private float drainRate = 5f;

    [Header("Realistic Low Power Flicker Settings", order = 8)]
    [Space(5)]
    [Range(0.01f, 0.20f)][SerializeField] private float flickerThresholdPercent = 0.10f;
    [SerializeField] private float baseLightIntensity = 1.5f;
    [SerializeField] private float flickerSpeed = 8f;
    [Range(0f, 1f)][SerializeField] private float dropToBlackChance = 0.35f;

    [Header("Prompt Timings & Anti-Spam", order = 9)]
    [Space(5)]
    [SerializeField] private float promptDuration = 4f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float toggleCooldown = 0.25f;
    [SerializeField] private float emptyPromptCooldown = 0.8f;

    [Header("Toggle On/Off Prompts", order = 10)]
    [Space(5)]
    [TextArea][SerializeField] private string keyboardTogglePrompt = "[F] to toggle On/Off";
    [TextArea][SerializeField] private string xboxTogglePrompt = "Press <sprite name=\"Right Bumper\"> to toggle On/Off";
    [TextArea][SerializeField] private string playStationTogglePrompt = "Press <sprite name=\"R1\"> to toggle On/Off";

    [Header("Equipment Wheel Prompts", order = 11)]
    [Space(5)]
    [TextArea][SerializeField] private string keyboardWheelPrompt = "Hold [TAB] + Move [Mouse]\nRelease [TAB] to equip item";
    [TextArea][SerializeField] private string xboxWheelPrompt = "Hold <sprite name=\"Left Bumper\"> + Move <sprite name=\"Right Stick\">\nRelease <sprite name=\"Left Bumper\"> to equip item";
    [TextArea][SerializeField] private string playStationWheelPrompt = "Hold <sprite name=\"L1\"> + Move <sprite name=\"_Right Stick\">\nRelease <sprite name=\"L1\"> to equip item";

    public GameObject torchLightZone;

    private static readonly int ChangeTrigger = Animator.StringToHash("Change");
    private static readonly int ReturnTrigger = Animator.StringToHash("Return");

    private ToolClass toolData;
    private float maxPower;
    private float currentBatteryPower;
    private float singleBatteryMaxPower;

    public bool requiresBatteryChange;

    private Transform flashlightAnchor;
    private Light torchLight;
    private FlashlightUIScript flashlightUI;
    private TextMeshProUGUI promptTextUI;
    private CanvasGroup promptCanvasGroup;

    private bool isOn;
    private bool isEquipped;
    private Color emissionStart;

    private float bobTimer;
    private Vector3 currentSwayOffset;

    private InputHandler inputHandler;
    private CharacterController characterController;
    private PlayerInput playerInput;

    private static bool hasShownTorchPromptOnce = false;

    private Coroutine flickerCoroutine;
    private Coroutine tutorialSequenceCoroutine;

    private InputDeviceType currentDevice = InputDeviceType.KeyboardMouse;
    private bool isShowingTogglePrompt;
    private bool isShowingWheelPrompt;
    private bool wasAttemptingBatteryChange;
    private bool wasLowPowerState;

    private float lastToggleTime = -999f;
    private float lastEmptyPromptTime = -999f;

    public bool IsOn => isOn;
    public float CurrentPower => currentBatteryPower;
    public float MaxPower => maxPower;
    public bool UsesPower => toolData != null && toolData.usesPower;

    private void Awake()
    {
        FindFlashlightUI();
        SetupFlashlightAnchor();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (flashlightAudioSource == null)
        {
            flashlightAudioSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        if (isEquipped)
        {
            SetBatteryUIVisible(true);
            if (isOn && currentBatteryPower > 0f && !requiresBatteryChange)
            {
                TurnOnLightState();
            }
        }
    }

    private void OnDisable()
    {
        ResetAnimationState();
        TurnOffLightState();
        SetBatteryUIVisible(false);

        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
    }

    private void OnDestroy()
    {
        UnbindHoldEvents();

        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
    }

    private void Update()
    {
        if (inputHandler != null && inputHandler.toggleFlashlight)
        {
            Toggle();
            inputHandler.toggleFlashlight = false;
        }

        if (!isEquipped) return;

        HandleBatteryInteraction();
        HandleBatteryDrain();
    }

    private void LateUpdate()
    {
        if (TorchLight == null || flashlightAnchor == null) return;

        Vector3 targetPosition = flashlightAnchor.position + CalculateBobOffset();
        Quaternion targetRotation = flashlightAnchor.rotation * CalculateSwayRotation();

        TorchLight.transform.position = Vector3.Lerp(TorchLight.transform.position, targetPosition, Time.deltaTime * positionLagSpeed);
        TorchLight.transform.rotation = Quaternion.Slerp(TorchLight.transform.rotation, targetRotation, Time.deltaTime * rotationLagSpeed);
    }

    public void Initialise(ToolClass tool)
    {
        toolData = tool;
        maxPower = tool.maxPower;
        singleBatteryMaxPower = maxPower;
        currentBatteryPower = singleBatteryMaxPower;
        requiresBatteryChange = false;
        wasLowPowerState = false;

        SetupFlashlightAnchor();

        if (TorchLight != null)
        {
            torchLight = TorchLight.GetComponent<Light>();
            if (torchLight != null)
            {
                baseLightIntensity = torchLight.intensity;
                if (lightCookie != null) torchLight.cookie = lightCookie;
            }
        }

        if (glassRenderer != null)
        {
            emissionStart = glassRenderer.material.GetColor("_EmissionColor");
        }

        FindFlashlightUI();

        if (!hasShownTorchPromptOnce)
        {
            hasShownTorchPromptOnce = true;
            if (InputDeviceDetector.Instance != null)
            {
                currentDevice = InputDeviceDetector.Instance.CurrentDevice;
                InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
            }
            if (tutorialSequenceCoroutine != null) StopCoroutine(tutorialSequenceCoroutine);
            tutorialSequenceCoroutine = StartCoroutine(FullTutorialSequenceRoutine());
        }
    }

    public void SetEquippedState(bool equipped)
    {
        isEquipped = equipped;
        SetBatteryUIVisible(equipped);

        if (!equipped)
        {
            ResetAnimationState();
            TurnOffLightState();
        }
        else
        {
            isOn = true;
            if (currentBatteryPower > 0f && !requiresBatteryChange)
            {
                TurnOnLightState();
            }
        }
    }

    public void Toggle()
    {
        if (!isEquipped || Time.time < lastToggleTime + toggleCooldown) return;

        lastToggleTime = Time.time;

        if (currentBatteryPower <= 0f && requiresBatteryChange)
        {
            TriggerEmptyFeedback();
            return;
        }

        if (torchLight == null) return;

        isOn = !isOn;

        if (flashlightAudioSource != null && torchClickClip != null)
        {
            flashlightAudioSource.PlayOneShot(torchClickClip);
        }

        if (isOn)
        {
            TurnOnLightState();
        }
        else
        {
            TurnOffLightState();
        }
    }

    public void ReplaceBatteryFromInventory()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (inventoryManager != null && batteryItemData != null)
        {
            if (inventoryManager.RemoveItem(batteryItemData, 1))
            {
                currentBatteryPower = singleBatteryMaxPower;
                requiresBatteryChange = false;
                wasLowPowerState = false;

                if (isOn) TurnOnLightState();

                UpdateBatteryUI();
            }
        }
    }

    private int GetCurrentInventoryBatteryCount()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (inventoryManager == null || batteryItemData == null) return 0;

        return inventoryManager.GetItemCount(batteryItemData);
    }

    private void HandleBatteryInteraction()
    {
        if (holdInteraction == null || inputHandler == null) return;

        int currentBatteries = GetCurrentInventoryBatteryCount();
        bool hasSpareBattery = currentBatteries > 0;
        bool isLowPower = (currentBatteryPower / singleBatteryMaxPower) <= flickerThresholdPercent;
        bool needsBattery = requiresBatteryChange || isLowPower;

        if (inputHandler.changeBattery)
        {
            if (hasSpareBattery)
            {
                holdInteraction.BeginHold();
            }
            else if (!wasAttemptingBatteryChange && needsBattery)
            {
                TriggerEmptyFeedback();
            }
            wasAttemptingBatteryChange = true;
        }
        else
        {
            if (holdInteraction.IsHolding)
            {
                holdInteraction.CancelHold();
            }
            wasAttemptingBatteryChange = false;
        }
    }

    private void HandleBatteryDrain()
    {
        if (!isOn || requiresBatteryChange) return;

        currentBatteryPower = Mathf.Clamp(currentBatteryPower - (drainRate * Time.deltaTime), 0f, singleBatteryMaxPower);

        UpdateBatteryUI();

        if (flickerCoroutine == null) UpdateEmission();

        CheckFlickerState();

        if (currentBatteryPower <= 0f)
        {
            requiresBatteryChange = true;
            TurnOffLightState();
            UpdateBatteryUI();
        }
    }

    private void TriggerEmptyFeedback()
    {
        if (Time.time < lastEmptyPromptTime + emptyPromptCooldown) return;

        lastEmptyPromptTime = Time.time;

        if (flashlightAudioSource != null && batteryEmptyClip != null)
        {
            flashlightAudioSource.PlayOneShot(batteryEmptyClip);
        }

        if (flashlightUI != null) flashlightUI.TriggerEmptyFlash();
    }

    private void OnHoldBatteryChangeStart()
    {
        if (animator != null)
        {
            animator.ResetTrigger(ReturnTrigger);
            animator.SetTrigger(ChangeTrigger);
        }

        if (flashlightAudioSource != null && batteryChangeClip != null)
        {
            flashlightAudioSource.PlayOneShot(batteryChangeClip);
        }
    }

    private void OnHoldBatteryChangeCancelled()
    {
        ResetAnimationState();
    }

    private void OnHoldBatteryChangeComplete()
    {
        if (inputHandler != null) inputHandler.changeBattery = false;

        ResetAnimationState();
        ReplaceBatteryFromInventory();
    }

    private void ResetAnimationState()
    {
        if (animator != null)
        {
            animator.ResetTrigger(ChangeTrigger);
            animator.SetTrigger(ReturnTrigger);
        }
    }

    private void TurnOnLightState()
    {
        SetLightActive(true);
        UpdateEmission();
        CheckFlickerState();
    }

    private void TurnOffLightState()
    {
        StopFlicker();
        SetLightActive(false);
        OffEmission();
    }

    private void SetLightActive(bool active)
    {
        if (torchLight != null)
        {
            torchLight.enabled = active;
            if (active) torchLight.intensity = baseLightIntensity;
        }

        if (torchLightZone != null) torchLightZone.SetActive(active);
    }

    private void CheckFlickerState()
    {
        if (!isOn || currentBatteryPower <= 0f || requiresBatteryChange)
        {
            StopFlicker();
            return;
        }

        if ((currentBatteryPower / singleBatteryMaxPower) <= flickerThresholdPercent)
        {
            if (flickerCoroutine == null) flickerCoroutine = StartCoroutine(RealisticFlickerRoutine());
        }
        else
        {
            StopFlicker();
            if (torchLight != null) torchLight.intensity = baseLightIntensity;
        }
    }

    private IEnumerator RealisticFlickerRoutine()
    {
        float noiseSeed = Random.Range(0f, 100f);

        while (isOn && currentBatteryPower > 0f && !requiresBatteryChange)
        {
            float powerPercent = currentBatteryPower / singleBatteryMaxPower;
            float tension = 1f - Mathf.Clamp01(powerPercent / flickerThresholdPercent);

            if (Random.value < dropToBlackChance * tension)
            {
                SetLightActive(false);
                yield return new WaitForSeconds(Random.Range(0.04f, Mathf.Lerp(0.08f, 0.35f, tension)));
                if (!isOn || currentBatteryPower <= 0f || requiresBatteryChange) break;
                SetLightActive(true);
            }

            float duration = Random.Range(0.05f, 0.18f);
            float elapsed = 0f;
            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed);
            float targetIntensity = Mathf.Lerp(baseLightIntensity * 0.05f, Mathf.Lerp(baseLightIntensity, baseLightIntensity * 0.4f, tension), noise);
            float initialIntensity = torchLight != null ? torchLight.intensity : targetIntensity;

            while (elapsed < duration)
            {
                if (!isOn || currentBatteryPower <= 0f || requiresBatteryChange) yield break;

                elapsed += Time.deltaTime;
                float currentIntensity = Mathf.Lerp(initialIntensity, targetIntensity, elapsed / duration);

                if (torchLight != null) torchLight.intensity = currentIntensity;

                if (glassRenderer != null)
                {
                    glassRenderer.material.SetColor("_EmissionColor", emissionStart * (currentIntensity / baseLightIntensity));
                }

                yield return null;
            }

            yield return new WaitForSeconds(Random.Range(0.05f, Mathf.Lerp(0.4f, 0.1f, tension)));
        }

        flickerCoroutine = null;
    }

    private void StopFlicker()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
    }

    private Vector3 CalculateBobOffset()
    {
        if (characterController != null && characterController.isGrounded && new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude > 0.1f)
        {
            bobTimer += Time.deltaTime * walkBobSpeed;
            return flashlightAnchor.right * (Mathf.Sin(bobTimer) * walkBobAmountX) + flashlightAnchor.up * (Mathf.Cos(bobTimer * 2f) * walkBobAmountY);
        }

        bobTimer = 0f;
        return Vector3.zero;
    }

    private Quaternion CalculateSwayRotation()
    {
        Vector2 lookInput = (inputHandler != null && !inputHandler.blockLookInput) ? inputHandler.look : Vector2.zero;
        Vector3 targetSway = new Vector3(-Mathf.Clamp(lookInput.y * swayAmount, -maxSwayAmount, maxSwayAmount), Mathf.Clamp(lookInput.x * swayAmount, -maxSwayAmount, maxSwayAmount), 0f);
        currentSwayOffset = Vector3.Lerp(currentSwayOffset, targetSway, Time.deltaTime * swaySmoothness);
        return Quaternion.Euler(currentSwayOffset);
    }

    private void SetupFlashlightAnchor()
    {
        if (fpsController == null) fpsController = FindFirstObjectByType<FPSController>();

        if (fpsController != null)
        {
            inputHandler = fpsController.InputHandler;
            characterController = fpsController.Controller;
            playerInput = fpsController.GetComponent<PlayerInput>();
            if (fpsController.FlashlightAnchor != null) flashlightAnchor = fpsController.FlashlightAnchor;
        }
    }

    private void FindFlashlightUI()
    {
        if (flashlightUI == null)
        {
            flashlightUI = FindFirstObjectByType<FlashlightUIScript>(FindObjectsInactive.Include);
        }

        if (flashlightUI != null)
        {
            promptTextUI = flashlightUI.TutorialPromptText;
            promptCanvasGroup = flashlightUI.TutorialPromptCanvasGroup;

            if (holdInteraction != flashlightUI.HoldInteraction)
            {
                UnbindHoldEvents();

                holdInteraction = flashlightUI.HoldInteraction;

                BindHoldEvents();
            }
        }
    }

    private void BindHoldEvents()
    {
        if (holdInteraction == null) return;

        holdInteraction.onHoldStarted.AddListener(OnHoldBatteryChangeStart);
        holdInteraction.onHoldCancelled.AddListener(OnHoldBatteryChangeCancelled);
        holdInteraction.onHoldCompleted.AddListener(OnHoldBatteryChangeComplete);
    }

    private void UnbindHoldEvents()
    {
        if (holdInteraction == null) return;

        holdInteraction.onHoldStarted.RemoveListener(OnHoldBatteryChangeStart);
        holdInteraction.onHoldCancelled.RemoveListener(OnHoldBatteryChangeCancelled);
        holdInteraction.onHoldCompleted.RemoveListener(OnHoldBatteryChangeComplete);
    }

    private void SetBatteryUIVisible(bool visible)
    {
        FindFlashlightUI();
        if (flashlightUI != null) flashlightUI.SetUIVisibility(visible);
        if (visible) UpdateBatteryUI();
    }

    private void UpdateBatteryUI()
    {
        if (flashlightUI == null) return;

        int spareBatteries = GetCurrentInventoryBatteryCount();

        int activeBatteryCount = (currentBatteryPower > 0f && !requiresBatteryChange) ? 1 : 0;

        int totalBatteryCount = activeBatteryCount + spareBatteries;

        bool isLowPower = (currentBatteryPower / singleBatteryMaxPower) <= flickerThresholdPercent;
        bool hasSpareBattery = spareBatteries > 0;
        bool shouldShowPrompt = isEquipped && (requiresBatteryChange || isLowPower);

        if (shouldShowPrompt && !wasLowPowerState && !hasSpareBattery) TriggerEmptyFeedback();

        wasLowPowerState = shouldShowPrompt;

        flashlightUI.UpdateBatteryUI(totalBatteryCount, currentBatteryPower <= 0f);
        flashlightUI.SetBatteryPromptState(shouldShowPrompt, hasSpareBattery);
    }

    private IEnumerator FullTutorialSequenceRoutine()
    {
        yield return null;

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) yield return null;

        isShowingTogglePrompt = true;
        yield return StartCoroutine(FadePromptRoutine(GetTogglePromptString()));
        isShowingTogglePrompt = false;

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) yield return null;

        isShowingWheelPrompt = true;
        yield return StartCoroutine(FadePromptRoutine(GetWheelPromptString()));
        isShowingWheelPrompt = false;

        tutorialSequenceCoroutine = null;
    }

    private IEnumerator FadePromptRoutine(string message)
    {
        if (promptTextUI == null || promptCanvasGroup == null) yield break;

        promptTextUI.text = message;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            promptCanvasGroup.alpha = Mathf.Lerp(promptCanvasGroup.alpha, 1f, timer / fadeDuration);
            yield return null;
        }

        promptCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(promptDuration);

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            promptCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        promptCanvasGroup.alpha = 0f;
        promptTextUI.text = "";
    }

    private string GetTogglePromptString()
    {
        return currentDevice switch
        {
            InputDeviceType.Xbox => xboxTogglePrompt,
            InputDeviceType.PlayStation => playStationTogglePrompt,
            _ => keyboardTogglePrompt
        };
    }

    private string GetWheelPromptString()
    {
        if (playerInput != null)
        {
            string scheme = playerInput.currentControlScheme;
            if (scheme == "PlayStation" || scheme == "PS4" || scheme == "PS5") return playStationWheelPrompt;
            if (scheme == "Xbox" || scheme == "Gamepad") return xboxWheelPrompt;
        }
        return keyboardWheelPrompt;
    }

    private void HandleDeviceChanged(InputDeviceType newDevice)
    {
        currentDevice = newDevice;
        if (promptTextUI == null) return;

        if (isShowingTogglePrompt) promptTextUI.text = GetTogglePromptString();
        else if (isShowingWheelPrompt) promptTextUI.text = GetWheelPromptString();
    }

    private void UpdateEmission()
    {
        if (glassRenderer != null)
        {
            glassRenderer.material.SetColor("_EmissionColor", emissionStart * (currentBatteryPower / singleBatteryMaxPower));
        }
    }

    private void OffEmission()
    {
        if (glassRenderer != null)
        {
            glassRenderer.material.SetColor("_EmissionColor", Color.black);
        }
    }
}