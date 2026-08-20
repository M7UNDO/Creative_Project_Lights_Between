using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class GeneratorPowerSystem : MonoBehaviour, IInteractable, IToolPower
{
    [Header("Generator State")]
    [SerializeField] private bool hasFuel;
    [SerializeField] private bool isPoweredOn;

    [Header("Power Settings")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float baseFuelDrainRate = 0.14f;
    [SerializeField] private ElectricBoxBreaker connectedBreaker;
    private float currentFuel;

    [Header("Hold Interaction")]
    [SerializeField] private HoldInteraction holdInteraction;

    [Header("Fuel Mini Game")]
    [SerializeField] private GeneratorFuelMiniGame fuelMiniGame;

    [Header("Prompt Messages")]
    [SerializeField] private string noFuelPrompt = "Equip petrol can to add fuel";
    [SerializeField] private string startPrompt = "Hold to start generator";
    [SerializeField] private string poweredPrompt = "Generator is running";

    [Header("Events")]
    public UnityEvent onGeneratorStarted;
    public UnityEvent onGeneratorStopped;
    public UnityEvent onFuelAdded;

    public string promptMessage;

    [Header("Generator Audio")]
    [SerializeField] private AudioSource generatorAudioSource;
    [SerializeField] private AudioClip generatorRunningSFX;
    [SerializeField] private AudioClip generatorStartSFX;
    [SerializeField] private AudioClip generatorFailSFX;

    private InputHandler currentInput;
    private Coroutine audioTransitionCoroutine;
    private ToolEquipmentManager playerEquipmentManager;
    private PlayerToolInventory playerInventory;

    public float CurrentPower => currentFuel;
    public float MaxPower => maxFuel;
    public bool UsesPower => isPoweredOn;
    public bool IsPowerOn => isPoweredOn;

    public float CurrentTotalDrainRate
    {
        get;
        private set;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerEquipmentManager = player.GetComponent<ToolEquipmentManager>();
            playerInventory = player.GetComponent<PlayerToolInventory>();
        }

        if (connectedBreaker == null)
        {
            connectedBreaker = Object.FindFirstObjectByType<ElectricBoxBreaker>();
        }

        currentFuel = hasFuel ? maxFuel : 0f;

        UpdatePrompt();

        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.AddListener(TurnGeneratorOn);
        }

        if (fuelMiniGame != null)
        {
            fuelMiniGame.OnFuelMiniGameCompleted += AddFuel;
        }

        if (generatorAudioSource == null)
        {
            generatorAudioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.RemoveListener(TurnGeneratorOn);
        }

        if (fuelMiniGame != null)
        {
            fuelMiniGame.OnFuelMiniGameCompleted -= AddFuel;
        }
    }

    private void Update()
    {
        if (isPoweredOn)
        {
            float additionalDrain = (connectedBreaker != null) ? connectedBreaker.GetTotalActiveLightDrain() : 0f;
            CurrentTotalDrainRate = baseFuelDrainRate + additionalDrain;

            currentFuel -= CurrentTotalDrainRate * Time.deltaTime;
            currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

            if (currentFuel <= 0f)
            {
                hasFuel = false;
                TurnGeneratorOff();
            }
        }

        if (currentInput == null) return;
        if (isPoweredOn) return;
        if (!hasFuel) return;
        if (holdInteraction == null) return;

        if (currentInput.interact)
        {
            holdInteraction.BeginHold();
        }
        else
        {
            holdInteraction.CancelHold();
        }
    }

    public void Interact()
    {
        if (isPoweredOn)
        {
            UpdatePrompt();
            return;
        }

        if (!hasFuel)
        {
            if (playerEquipmentManager == null || playerInventory == null)
            {
                UpdatePrompt();
                return;
            }

            bool isHoldingFuel = playerEquipmentManager.CurrentTool != null &&
                                playerEquipmentManager.CurrentTool.toolType == ToolType.GeneratorFuel;

            if (isHoldingFuel)
            {
                StartFuelMiniGame();
                return;
            }

            if (playerInventory.HasToolType(ToolType.GeneratorFuel))
            {
                ToolClass fuelTool = playerInventory.GetToolByType(ToolType.GeneratorFuel);
                playerEquipmentManager.EquipTool(fuelTool);
                StartFuelMiniGame();
                return;
            }

            UpdatePrompt();
            return;
        }

      InputHandler input = FindFirstObjectByType<InputHandler>();

        if (input != null)
        {
            currentInput = input;
        }
    }

    private void StartFuelMiniGame()
    {
        if (fuelMiniGame != null)
        {
            fuelMiniGame.StartMiniGame();
        }
    }

    private void AddFuel()
    {
        hasFuel = true;
        currentFuel = maxFuel;

        if (playerEquipmentManager != null && playerInventory != null)
        {
            ToolClass fuelTool = playerEquipmentManager.CurrentTool;
            playerEquipmentManager.UnequipTool();
            playerInventory.RemoveTool(fuelTool);
        }

        onFuelAdded?.Invoke();
        UpdatePrompt();
    }

    private void TurnGeneratorOn()
    {
        isPoweredOn = true;
        currentInput = null;

        UpdatePrompt();

        if (audioTransitionCoroutine != null)
        {
            StopCoroutine(audioTransitionCoroutine);
        }
        audioTransitionCoroutine = StartCoroutine(PlayStartupSequence());

        onGeneratorStarted?.Invoke();
    }

    public void TurnGeneratorOff()
    {
        isPoweredOn = false;

        if (holdInteraction != null)
        {
            holdInteraction.ResetHold();
        }

        UpdatePrompt();

        if (audioTransitionCoroutine != null)
        {
            StopCoroutine(audioTransitionCoroutine);
            audioTransitionCoroutine = null;
        }

        if (generatorAudioSource != null)
        {
            generatorAudioSource.Stop();
            if (generatorFailSFX != null)
            {
                generatorAudioSource.clip = generatorFailSFX;
                generatorAudioSource.loop = false;
                generatorAudioSource.Play();
            }
        }

        onGeneratorStopped?.Invoke();
    }

    private System.Collections.IEnumerator PlayStartupSequence()
    {
        if (generatorAudioSource != null)
        {
            if (generatorStartSFX != null)
            {
                generatorAudioSource.clip = generatorStartSFX;
                generatorAudioSource.loop = false;
                generatorAudioSource.Play();
                yield return new WaitForSeconds(generatorStartSFX.length);
            }

            if (isPoweredOn && generatorRunningSFX != null)
            {
                generatorAudioSource.clip = generatorRunningSFX;
                generatorAudioSource.loop = true;
                generatorAudioSource.Play();
            }
        }
    }

    private void UpdatePrompt()
    {
        if (isPoweredOn)
        {
            promptMessage = poweredPrompt;
        }
        else if (!hasFuel)
        {
            promptMessage = noFuelPrompt;
        }
        else
        {
            promptMessage = startPrompt;
        }
    }
}