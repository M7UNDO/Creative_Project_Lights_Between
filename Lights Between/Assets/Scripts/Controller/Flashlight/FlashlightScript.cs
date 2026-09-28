using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightScript : MonoBehaviour, IToolPower
{
    [Header("References")]
    [SerializeField] private GameObject TorchLight;
    [SerializeField] private AudioSource torchClickSFX;
    [SerializeField] private FPSController fpsController;
    [SerializeField] private HoldInteraction holdInteraction;

    [Header("Visuals")]
    [SerializeField] private Texture lightCookie;
    [SerializeField] private Renderer glassRenderer;

    [Header("Audio")]
    [SerializeField] private AudioSource batteryChangeSFX;
    [SerializeField] private AudioSource batteryEmptySFX;

    [Header("Lag & Tracking")]
    [SerializeField] private float positionLagSpeed = 12f;
    [SerializeField] private float rotationLagSpeed = 8f;

    [Header("Mouse/Stick Sway Settings")]
    [SerializeField] private float swayAmount = 1.5f;
    [SerializeField] private float maxSwayAmount = 4f;
    [SerializeField] private float swaySmoothness = 6f;

    [Header("Movement Bobbing Settings")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmountX = 0.025f;
    [SerializeField] private float walkBobAmountY = 0.025f;

    [Header("Drain Settings")]
    [SerializeField] private float drainRate = 5f;

    [Header("Realistic Low Power Flicker Settings")]
    [Range(0.01f, 0.20f)]
    [SerializeField] private float flickerThresholdPercent = 0.10f;
    [SerializeField] private float baseLightIntensity = 1.5f;
    [SerializeField] private float flickerSpeed = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float dropToBlackChance = 0.35f;

    [Header("Prompt Timings")]
    [SerializeField] private float promptDuration = 4f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Toggle On/Off Prompts")]
    [TextArea][SerializeField] private string keyboardTogglePrompt = "[F] to toggle On/Off";
    [TextArea][SerializeField] private string xboxTogglePrompt = "Press <sprite name=\"Right Bumper\"> to toggle On/Off";
    [TextArea][SerializeField] private string playStationTogglePrompt = "Press <sprite name=\"R1\"> to toggle On/Off";

    [Header("Equipment Wheel Prompts")]
    [TextArea][SerializeField] private string keyboardWheelPrompt = "Hold [TAB] + Move [Mouse]\nRelease [TAB] to equip item";
    [TextArea][SerializeField] private string xboxWheelPrompt = "Hold <sprite name=\"Left Bumper\"> + Move <sprite name=\"Right Stick\">\nRelease <sprite name=\"Left Bumper\"> to equip item";
    [TextArea][SerializeField] private string playStationWheelPrompt = "Hold <sprite name=\"L1\"> + Move <sprite name=\"_Right Stick\">\nRelease <sprite name=\"L1\"> to equip item";

    private ToolClass toolData;
    private float maxPower;

    private int batteryCount = 1;
    private const int MaxBatteries = 3;
    private float currentBatteryPower;
    private float singleBatteryMaxPower;

    public bool requiresBatteryChange;

    private Transform flashlightAnchor;
    private Light torchLight;
    public GameObject torchLightZone;

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

    public float CurrentPower => (batteryCount > 0 ? currentBatteryPower : 0f) + (Mathf.Max(0, batteryCount - 1) * singleBatteryMaxPower);
    public float MaxPower => maxPower;
    public bool UsesPower => toolData != null && toolData.usesPower;

    private void Awake()
    {
        FindFlashlightUI();
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
                if (holdInteraction != null)
                {
                    holdInteraction.onHoldCompleted.RemoveListener(OnHoldBatteryChangeComplete);
                }

                holdInteraction = flashlightUI.HoldInteraction;

                if (holdInteraction != null)
                {
                    holdInteraction.onHoldCompleted.AddListener(OnHoldBatteryChangeComplete);
                }
            }
        }
    }

    public void Initialise(ToolClass tool)
    {
        toolData = tool;

        maxPower = tool.maxPower;
        singleBatteryMaxPower = maxPower / (float)MaxBatteries;
        currentBatteryPower = singleBatteryMaxPower;
        batteryCount = 1;
        requiresBatteryChange = false;

        isEquipped = true;
        isOn = true;

        SetupFlashlightAnchor();
        SetupLiveLights();

        if (glassRenderer != null)
        {
            emissionStart = glassRenderer.material.GetColor("_EmissionColor");
        }

        FindFlashlightUI();
        TurnOnLightState();
        SetBatteryUIVisible(true);
        UpdateBatteryUI();

        if (!hasShownTorchPromptOnce)
        {
            hasShownTorchPromptOnce = true;
            StartTutorialSequence();
        }
    }

    private void SetupFlashlightAnchor()
    {
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<FPSController>();
        }

        if (fpsController != null)
        {
            inputHandler = fpsController.InputHandler;
            characterController = fpsController.Controller;
            playerInput = fpsController.GetComponent<PlayerInput>();

            if (fpsController.FlashlightAnchor != null)
            {
                flashlightAnchor = fpsController.FlashlightAnchor;
            }
        }
    }

    private void SetupLiveLights()
    {
        if (TorchLight != null)
        {
            torchLight = TorchLight.GetComponent<Light>();

            if (torchLight != null)
            {
                baseLightIntensity = torchLight.intensity;

                if (lightCookie != null)
                {
                    torchLight.cookie = lightCookie;
                }
            }
        }
    }

    private void StartTutorialSequence()
    {
        if (InputDeviceDetector.Instance != null)
        {
            currentDevice = InputDeviceDetector.Instance.CurrentDevice;
            InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
        }

        if (tutorialSequenceCoroutine != null)
        {
            StopCoroutine(tutorialSequenceCoroutine);
        }

        tutorialSequenceCoroutine = StartCoroutine(FullTutorialSequenceRoutine());
    }

    private IEnumerator FullTutorialSequenceRoutine()
    {
        yield return null;

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return null;
        }

        isShowingTogglePrompt = true;
        yield return StartCoroutine(FadePromptRoutine(GetTogglePromptString()));
        isShowingTogglePrompt = false;

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return null;
        }

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
        float startAlpha = promptCanvasGroup.alpha;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            promptCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, timer / fadeDuration);
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
        switch (currentDevice)
        {
            case InputDeviceType.Xbox:
                return xboxTogglePrompt;
            case InputDeviceType.PlayStation:
                return playStationTogglePrompt;
            default:
                return keyboardTogglePrompt;
        }
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

        if (isShowingTogglePrompt)
        {
            promptTextUI.text = GetTogglePromptString();
        }
        else if (isShowingWheelPrompt)
        {
            promptTextUI.text = GetWheelPromptString();
        }
    }

    public void SetEquippedState(bool equipped)
    {
        isEquipped = equipped;
        SetBatteryUIVisible(equipped);

        if (!equipped)
        {
            TurnOffLightState();
        }
        else if (isOn && currentBatteryPower > 0f && !requiresBatteryChange)
        {
            TurnOnLightState();
        }
    }

    private void SetBatteryUIVisible(bool visible)
    {
        FindFlashlightUI();

        if (flashlightUI != null)
        {
            flashlightUI.SetUIVisibility(visible);
        }

        if (visible)
        {
            UpdateBatteryUI();
        }
    }

    private void UpdateBatteryUI()
    {
        if (flashlightUI != null)
        {
            float powerPercent = singleBatteryMaxPower > 0f ? (currentBatteryPower / singleBatteryMaxPower) : 0f;
            bool isLowPower = powerPercent <= flickerThresholdPercent;
            bool hasSpareBattery = batteryCount > 1;
            bool shouldShowPrompt = isEquipped && (requiresBatteryChange || isLowPower);

            flashlightUI.UpdateBatteryUI(batteryCount, currentBatteryPower <= 0f);
            flashlightUI.SetBatteryPromptState(shouldShowPrompt, hasSpareBattery);
        }
    }

    public bool TryAddBattery()
    {
        if (batteryCount >= MaxBatteries) return false;

        batteryCount++;
        UpdateBatteryUI();

        if (requiresBatteryChange)
        {
            ChangeBattery();
        }

        return true;
    }

    private void ChangeBattery()
    {
        if (batteryCount <= 0) return;

        if (currentBatteryPower <= 0f)
        {
            batteryCount--;
        }

        if (batteryCount > 0)
        {
            currentBatteryPower = singleBatteryMaxPower;
            requiresBatteryChange = false;

            if (batteryChangeSFX != null)
            {
                batteryChangeSFX.Play();
            }

            if (isOn)
            {
                TurnOnLightState();
            }
        }

        UpdateBatteryUI();
    }

    public void Toggle()
    {
        if (!isEquipped) return;

        if (currentBatteryPower <= 0f && requiresBatteryChange)
        {
            if (batteryEmptySFX != null)
            {
                batteryEmptySFX.Play();
            }

            if (flashlightUI != null)
            {
                flashlightUI.TriggerEmptyFlash();
            }

            return;
        }

        if (torchLight == null) return;

        isOn = !isOn;

        if (torchClickSFX != null)
        {
            torchClickSFX.Play();
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

    private void OnHoldBatteryChangeComplete()
    {
        if (inputHandler != null)
        {
            inputHandler.changeBattery = false;
        }

        ChangeBattery();
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

        if (torchLightZone != null)
        {
            torchLightZone.SetActive(active);
        }
    }

    private void CheckFlickerState()
    {
        if (!isOn || currentBatteryPower <= 0f || requiresBatteryChange)
        {
            StopFlicker();
            return;
        }

        float powerPercent = currentBatteryPower / singleBatteryMaxPower;

        if (powerPercent <= flickerThresholdPercent)
        {
            if (flickerCoroutine == null)
            {
                flickerCoroutine = StartCoroutine(RealisticFlickerRoutine());
            }
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
            float normalizedTension = 1f - Mathf.Clamp01(powerPercent / flickerThresholdPercent);

            if (Random.value < dropToBlackChance * normalizedTension)
            {
                SetLightActive(false);
                OffEmission();

                float blackoutDuration = Random.Range(0.04f, Mathf.Lerp(0.08f, 0.35f, normalizedTension));
                yield return new WaitForSeconds(blackoutDuration);

                if (!isOn || currentBatteryPower <= 0f || requiresBatteryChange) break;

                SetLightActive(true);
            }

            float duration = Random.Range(0.05f, 0.18f);
            float elapsed = 0f;

            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed);
            float maxCap = Mathf.Lerp(baseLightIntensity, baseLightIntensity * 0.4f, normalizedTension);
            float minCap = baseLightIntensity * 0.05f;
            float targetIntensity = Mathf.Lerp(minCap, maxCap, noise);

            float initialIntensity = torchLight != null ? torchLight.intensity : targetIntensity;

            while (elapsed < duration)
            {
                if (!isOn || currentBatteryPower <= 0f || requiresBatteryChange) yield break;

                elapsed += Time.deltaTime;
                float currentTargetIntensity = Mathf.Lerp(initialIntensity, targetIntensity, elapsed / duration);

                if (torchLight != null)
                {
                    torchLight.intensity = currentTargetIntensity;
                }

                if (glassRenderer != null)
                {
                    float intensityRatio = currentTargetIntensity / baseLightIntensity;
                    glassRenderer.material.SetColor("_EmissionColor", emissionStart * intensityRatio);
                }

                yield return null;
            }

            float stableWait = Random.Range(0.05f, Mathf.Lerp(0.4f, 0.1f, normalizedTension));
            yield return new WaitForSeconds(stableWait);
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

    private void Update()
    {
        if (!isEquipped) return;

        if (holdInteraction != null && inputHandler != null)
        {
            bool hasSpareBattery = batteryCount > 1;

            if (inputHandler.changeBattery)
            {
                if (hasSpareBattery)
                {
                    holdInteraction.BeginHold();
                }
                else if (!wasAttemptingBatteryChange)
                {
                    if (batteryEmptySFX != null)
                    {
                        batteryEmptySFX.Play();
                    }

                    if (flashlightUI != null)
                    {
                        flashlightUI.TriggerEmptyFlash();
                    }
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

        if (!isOn || requiresBatteryChange) return;

        currentBatteryPower -= drainRate * Time.deltaTime;
        currentBatteryPower = Mathf.Clamp(currentBatteryPower, 0f, singleBatteryMaxPower);

        UpdateBatteryUI();

        if (flickerCoroutine == null)
        {
            UpdateEmission();
        }

        CheckFlickerState();

        if (currentBatteryPower <= 0f)
        {
            requiresBatteryChange = true;
            TurnOffLightState();
            UpdateBatteryUI();
        }
    }

    private void LateUpdate()
    {
        if (TorchLight == null || flashlightAnchor == null) return;

        Vector3 targetPosition = flashlightAnchor.position + CalculateBobOffset();
        Quaternion targetRotation = flashlightAnchor.rotation * CalculateSwayRotation();

        TorchLight.transform.position = Vector3.Lerp(
            TorchLight.transform.position,
            targetPosition,
            Time.deltaTime * positionLagSpeed
        );

        TorchLight.transform.rotation = Quaternion.Slerp(
            TorchLight.transform.rotation,
            targetRotation,
            Time.deltaTime * rotationLagSpeed
        );
    }

    private Vector3 CalculateBobOffset()
    {
        bool isMoving = false;

        if (characterController != null)
        {
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            isMoving = characterController.isGrounded && horizontalVelocity.magnitude > 0.1f;
        }

        if (isMoving)
        {
            bobTimer += Time.deltaTime * walkBobSpeed;
            float bobX = Mathf.Sin(bobTimer) * walkBobAmountX;
            float bobY = Mathf.Cos(bobTimer * 2f) * walkBobAmountY;

            return flashlightAnchor.right * bobX + flashlightAnchor.up * bobY;
        }

        bobTimer = 0f;
        return Vector3.zero;
    }

    private Quaternion CalculateSwayRotation()
    {
        Vector2 lookInput = Vector2.zero;

        if (inputHandler != null && !inputHandler.blockLookInput)
        {
            lookInput = inputHandler.look;
        }

        float swayX = lookInput.x * swayAmount;
        float swayY = lookInput.y * swayAmount;

        swayX = Mathf.Clamp(swayX, -maxSwayAmount, maxSwayAmount);
        swayY = Mathf.Clamp(swayY, -maxSwayAmount, maxSwayAmount);

        Vector3 targetSway = new Vector3(-swayY, swayX, 0f);
        currentSwayOffset = Vector3.Lerp(currentSwayOffset, targetSway, Time.deltaTime * swaySmoothness);

        return Quaternion.Euler(currentSwayOffset);
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
        TurnOffLightState();
        SetBatteryUIVisible(false);
        UnsubscribeDeviceDetector();
    }

    private void OnDestroy()
    {
        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.RemoveListener(OnHoldBatteryChangeComplete);
        }
        UnsubscribeDeviceDetector();
    }

    private void UnsubscribeDeviceDetector()
    {
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
    }

    private void UpdateEmission()
    {
        if (glassRenderer == null) return;

        float intensity = currentBatteryPower / singleBatteryMaxPower;
        glassRenderer.material.SetColor("_EmissionColor", emissionStart * intensity);
    }

    private void OffEmission()
    {
        if (glassRenderer == null) return;

        glassRenderer.material.SetColor("_EmissionColor", Color.black);
    }

    public void AddPower(float amount)
    {
        currentBatteryPower = Mathf.Clamp(currentBatteryPower + amount, 0f, singleBatteryMaxPower);

        if (currentBatteryPower > 0f && isOn)
        {
            requiresBatteryChange = false;
            TurnOnLightState();
        }

        UpdateBatteryUI();
    }
}