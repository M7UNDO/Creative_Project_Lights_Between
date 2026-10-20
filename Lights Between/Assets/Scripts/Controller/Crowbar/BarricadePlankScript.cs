using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BarricadePlankScript : MonoBehaviour, IInteractable
{
    [Header("Hold Interaction")]
    [SerializeField] private HoldInteraction holdInteraction;

    [Header("Prompt Messages")]
    [SerializeField] private string needCrowbarPrompt = "Equip crowbar to pry plank";
    [SerializeField] private string pryPrompt = "Hold to pry plank";

    [Header("Narrative/Objective Lock")]
    [SerializeField] private ObjectiveSO requiredObjective;

    [Header("Pop-off Physics")]
    [SerializeField] private float minPopForce = 4f;
    [SerializeField] private float maxPopForce = 7f;
    [SerializeField] private float torqueForce = 6f;
    [SerializeField] private float destroyDelay = 8f;

    [Header("SFX Clips")]
    [SerializeField] private AudioSource plankAudioSource;
    [SerializeField] private AudioClip pryAudioClip;
    [SerializeField] private AudioClip popAudioClip;
    [SerializeField] private AudioClip impactAudioClip;

    [HideInInspector] public string promptMessage;

    private Rigidbody rb;
    private Collider plankCollider;
    private bool isDetached;
    public bool IsDetached => isDetached;
    private bool isPryingSFXPlaying;

    private InputHandler currentInput;
    private ToolEquipmentManager playerEquipmentManager;
    private PlayerToolInventory playerInventory;

    private void Awake()
    {
        plankCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;

        if (plankAudioSource == null)
        {
            plankAudioSource = GetComponent<AudioSource>();
            if (plankAudioSource == null)
            {
                plankAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void Start()
    {
        ResolvePlayerReferences();

        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.AddListener(PryOff);
            holdInteraction.onHoldStarted.AddListener(StartPryAudio);
        }

        UpdatePrompt();
    }

    private void OnDestroy()
    {
        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.RemoveListener(PryOff);
            holdInteraction.onHoldStarted.RemoveListener(StartPryAudio);
        }
    }

    private void Update()
    {
        if (isDetached || !IsPlankUnlocked()) return;
        if (currentInput == null) return;
        if (holdInteraction == null) return;

        if (currentInput.interact)
        {
            holdInteraction.BeginHold();
        }
        else
        {
            CancelInteract();
        }
    }

    private void ResolvePlayerReferences()
    {
        if (playerInventory == null)
        {
            playerInventory = Object.FindFirstObjectByType<PlayerToolInventory>();
        }

        if (playerEquipmentManager == null)
        {
            playerEquipmentManager = Object.FindFirstObjectByType<ToolEquipmentManager>();
        }
    }

    public bool IsPlankUnlocked()
    {
        if (requiredObjective == null) return true;

        if (ObjectiveManager.Instance != null)
        {
            return ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) == ObjectiveStatus.Active;
        }

        return false;
    }

    public bool CanInteract()
    {
        if (isDetached || !IsPlankUnlocked()) return false;

        ResolvePlayerReferences();

        if (playerEquipmentManager == null || playerInventory == null) return false;

        bool isHoldingCrowbar = playerEquipmentManager.CurrentTool != null &&
                                playerEquipmentManager.CurrentTool.toolType == ToolType.Crowbar;

        return isHoldingCrowbar || playerInventory.HasToolType(ToolType.Crowbar);
    }

    public void Interact()
    {
        if (isDetached || !IsPlankUnlocked()) return;

        ResolvePlayerReferences();

        if (playerEquipmentManager == null || playerInventory == null)
        {
            UpdatePrompt();
            return;
        }

        bool isHoldingCrowbar = playerEquipmentManager.CurrentTool != null &&
                                playerEquipmentManager.CurrentTool.toolType == ToolType.Crowbar;

        if (!isHoldingCrowbar && playerInventory.HasToolType(ToolType.Crowbar))
        {
            ToolClass crowbarTool = playerInventory.GetToolByType(ToolType.Crowbar);
            playerEquipmentManager.EquipTool(crowbarTool);
            isHoldingCrowbar = true;
        }

        if (!isHoldingCrowbar)
        {
            UpdatePrompt();
            return;
        }

        InputHandler input = Object.FindFirstObjectByType<InputHandler>();
        if (input != null)
        {
            currentInput = input;
        }

        UpdatePrompt();
    }

    public void CancelInteract()
    {
        if (holdInteraction != null && holdInteraction.IsHolding)
        {
            holdInteraction.CancelHold();
        }

        StopPryAudio();
        currentInput = null;
    }

    private void StartPryAudio()
    {
        if (isPryingSFXPlaying || pryAudioClip == null || plankAudioSource == null) return;

        plankAudioSource.clip = pryAudioClip;
        plankAudioSource.loop = true;
        plankAudioSource.Play();
        isPryingSFXPlaying = true;
    }

    private void StopPryAudio()
    {
        if (!isPryingSFXPlaying || plankAudioSource == null) return;

        if (plankAudioSource.clip == pryAudioClip)
        {
            plankAudioSource.Stop();
            plankAudioSource.loop = false;
            plankAudioSource.clip = null;
        }

        isPryingSFXPlaying = false;
    }

    public void PryOff()
    {
        if (isDetached || !IsPlankUnlocked()) return;

        isDetached = true;
        currentInput = null;

        StopPryAudio();

        transform.SetParent(null);

        if (plankAudioSource != null && popAudioClip != null)
        {
            plankAudioSource.PlayOneShot(popAudioClip);
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 forceDirection = -transform.forward + Vector3.up * 0.4f;
        float randomForce = Random.Range(minPopForce, maxPopForce);
        rb.AddForce(forceDirection.normalized * randomForce, ForceMode.Impulse);

        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * torqueForce;

        rb.AddTorque(randomTorque, ForceMode.Impulse);

        if (impactAudioClip != null)
        {
            PlankImpactSFX impactHandler = gameObject.AddComponent<PlankImpactSFX>();
            impactHandler.Initialize(plankAudioSource, impactAudioClip);
        }

        StartCoroutine(CleanupRoutine());
    }

    private void UpdatePrompt()
    {
        if (!IsPlankUnlocked())
        {
            promptMessage = string.Empty;
            return;
        }

        ResolvePlayerReferences();

        bool hasCrowbarAccess = false;

        if (playerEquipmentManager != null && playerInventory != null)
        {
            bool isHoldingCrowbar = playerEquipmentManager.CurrentTool != null &&
                                    playerEquipmentManager.CurrentTool.toolType == ToolType.Crowbar;

            hasCrowbarAccess = isHoldingCrowbar || playerInventory.HasToolType(ToolType.Crowbar);
        }

        promptMessage = hasCrowbarAccess ? pryPrompt : needCrowbarPrompt;
    }

    private IEnumerator CleanupRoutine()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}