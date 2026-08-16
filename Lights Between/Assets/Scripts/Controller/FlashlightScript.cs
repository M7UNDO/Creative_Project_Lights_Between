using System.Collections;
using TMPro;
using UnityEngine;

public class FlashlightScript : MonoBehaviour, IToolPower
{
    [Header("References")]
    [SerializeField] private GameObject torchLightPrefab;
    [SerializeField] private AudioSource torchClickSFX;
    [SerializeField] private GameObject playerLightZonePrefab;
    [SerializeField] private FPSController fpsController;

    [Header("Visuals")]
    [SerializeField] private Texture lightCookie;
    [SerializeField] private Renderer glassRenderer;

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

    [Header("First Equip Prompt Settings")]
    [SerializeField] private float promptDuration = 4f;
    [SerializeField] private float fadeDuration = 0.5f;
    [TextArea]
    [SerializeField] private string keyboardTogglePrompt = "[F] to toggle On/Off";
    [TextArea]
    [SerializeField] private string xboxTogglePrompt = "Press <sprite name=\"Right Bumper\"> to toggle On/Off";
    [TextArea]
    [SerializeField] private string playStationTogglePrompt = "Press <sprite name=\"R1\"> to toggle On/Off";

    private ToolClass toolData;
    private float currentPower;
    private float maxPower;

    private Transform flashlightAnchor;
    private Light torchLight;
    private GameObject spawnedLightObject;
    private GameObject spawnedPlayerLightZone;
    public GameObject torchLightZone;

    private bool isOn;
    private bool isEquipped;
    private Color emissionStart;

    private float bobTimer;
    private Vector3 currentSwayOffset;

    private InputHandler inputHandler;
    private CharacterController characterController;

    private static bool hasShownTorchPromptOnce = false;

    private Coroutine promptCoroutine;
    private Coroutine flickerCoroutine;

    private InputDeviceType currentDevice = InputDeviceType.KeyboardMouse;
    private TMP_Text activePromptText;
    private CanvasGroup activePromptCanvasGroup;

    public float CurrentPower => currentPower;
    public float MaxPower => maxPower;
    public bool UsesPower => toolData != null && toolData.usesPower;

    public void Initialise(ToolClass tool)
    {
        toolData = tool;

        maxPower = tool.maxPower;
        currentPower = maxPower;

        isEquipped = true;
        isOn = true;

        SetupFlashlightAnchor();
        SpawnTorchLight();
        SpawnPlayerLightZone();

        if (glassRenderer != null)
        {
            emissionStart = glassRenderer.material.GetColor("_EmissionColor");
        }

        TurnOnLightState();

        if (!hasShownTorchPromptOnce)
        {
            hasShownTorchPromptOnce = true;
            //ShowFirstTimePrompt();
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

            if (fpsController.FlashlightAnchor != null)
            {
                flashlightAnchor = fpsController.FlashlightAnchor;
            }
        }
    }

    private void SpawnTorchLight()
    {
        if (torchLightPrefab == null || flashlightAnchor == null) return;

        spawnedLightObject = Instantiate(torchLightPrefab, flashlightAnchor.position, flashlightAnchor.rotation);
        torchLight = spawnedLightObject.GetComponentInChildren<Light>();

        if (torchLight == null)
        {
            Debug.LogWarning("Torch Light Prefab does not contain a Light component.");
            return;
        }

        baseLightIntensity = torchLight.intensity;

        if (lightCookie != null)
        {
            torchLight.cookie = lightCookie;
        }
    }

    private void SpawnPlayerLightZone()
    {
        if (playerLightZonePrefab == null || flashlightAnchor == null) return;

        Transform playerTransform = flashlightAnchor.root;

        spawnedPlayerLightZone = Instantiate(playerLightZonePrefab, playerTransform);
        spawnedPlayerLightZone.transform.localPosition = new Vector3(0f, 1f, 0f);
        spawnedPlayerLightZone.transform.localRotation = Quaternion.identity;
    }
    /*
    private void ShowFirstTimePrompt()
    {
        SimpleTutorialPromptManager promptManager = FindFirstObjectByType<SimpleTutorialPromptManager>();
        if (promptManager != null)
        {
            activePromptText = promptManager.tutorialText;
            activePromptCanvasGroup = promptManager.tutorialCanvasGroup;
        }

        if (InputDeviceDetector.Instance != null)
        {
            currentDevice = InputDeviceDetector.Instance.CurrentDevice;
            InputDeviceDetector.Instance.OnDeviceChanged += HandleDeviceChanged;
        }

        if (promptCoroutine != null)
        {
            StopCoroutine(promptCoroutine);
        }

        promptCoroutine = StartCoroutine(DisplayPromptRoutine());
    }*/

    private IEnumerator DisplayPromptRoutine()
    {
        if (activePromptText != null && activePromptCanvasGroup != null)
        {
            activePromptText.text = GetDevicePromptString();

            float timer = 0f;
            float startAlpha = activePromptCanvasGroup.alpha;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                activePromptCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, timer / fadeDuration);
                yield return null;
            }

            activePromptCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(promptDuration);

            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                activePromptCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }

            activePromptCanvasGroup.alpha = 0f;
            activePromptText.text = "";
        }

        UnsubscribeDeviceDetector();
    }

    private void HandleDeviceChanged(InputDeviceType newDevice)
    {
        currentDevice = newDevice;
        if (activePromptText != null)
        {
            activePromptText.text = GetDevicePromptString();
        }
    }

    private string GetDevicePromptString()
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

    private void UnsubscribeDeviceDetector()
    {
        if (InputDeviceDetector.Instance != null)
        {
            InputDeviceDetector.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
    }

    public void Toggle()
    {
        if (!isEquipped) return;
        if (currentPower <= 0f) return;
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

        if (spawnedPlayerLightZone != null)
        {
            spawnedPlayerLightZone.SetActive(active);
        }
    }

    private void CheckFlickerState()
    {
        if (!isOn || currentPower <= 0f)
        {
            StopFlicker();
            return;
        }

        float powerPercent = currentPower / maxPower;

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

        while (isOn && currentPower > 0f)
        {
            float powerPercent = currentPower / maxPower;
            float normalizedTension = 1f - Mathf.Clamp01(powerPercent / flickerThresholdPercent);

            if (Random.value < dropToBlackChance * normalizedTension)
            {
                SetLightActive(false);
                OffEmission();

                float blackoutDuration = Random.Range(0.04f, Mathf.Lerp(0.08f, 0.35f, normalizedTension));
                yield return new WaitForSeconds(blackoutDuration);

                if (!isOn || currentPower <= 0f) break;

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
                if (!isOn || currentPower <= 0f) yield break;

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
        if (!isOn) return;

        currentPower -= drainRate * Time.deltaTime;
        currentPower = Mathf.Clamp(currentPower, 0f, maxPower);

        if (flickerCoroutine == null)
        {
            UpdateEmission();
        }

        CheckFlickerState();

        if (currentPower <= 0f)
        {
            isOn = false;
            TurnOffLightState();
        }
    }

    private void LateUpdate()
    {
        if (spawnedLightObject == null || flashlightAnchor == null) return;

        Vector3 targetPosition = flashlightAnchor.position + CalculateBobOffset();
        Quaternion targetRotation = flashlightAnchor.rotation * CalculateSwayRotation();

        spawnedLightObject.transform.position = Vector3.Lerp(
            spawnedLightObject.transform.position,
            targetPosition,
            Time.deltaTime * positionLagSpeed
        );

        spawnedLightObject.transform.rotation = Quaternion.Slerp(
            spawnedLightObject.transform.rotation,
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
        if (isOn && currentPower > 0f)
        {
            TurnOnLightState();
        }
    }

    private void OnDisable()
    {
        TurnOffLightState();
        UnsubscribeDeviceDetector();
    }

    private void OnDestroy()
    {
        UnsubscribeDeviceDetector();

        if (spawnedLightObject != null)
        {
            Destroy(spawnedLightObject);
        }

        if (spawnedPlayerLightZone != null)
        {
            Destroy(spawnedPlayerLightZone);
        }
    }

    private void UpdateEmission()
    {
        if (glassRenderer == null) return;

        float intensity = currentPower / maxPower;
        glassRenderer.material.SetColor("_EmissionColor", emissionStart * intensity);
    }

    private void OffEmission()
    {
        if (glassRenderer == null) return;

        glassRenderer.material.SetColor("_EmissionColor", Color.black);
    }

    public void AddPower(float amount)
    {
        currentPower = Mathf.Clamp(currentPower + amount, 0f, maxPower);

        if (currentPower > 0f && isOn)
        {
            TurnOnLightState();
        }
    }
}