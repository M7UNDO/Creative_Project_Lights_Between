using UnityEngine;

public class PhoneScript : MonoBehaviour, IInteractable
{
    [Header("Screen Material References")]
    [SerializeField] private MeshRenderer phoneScreenRenderer;
    [SerializeField] private int screenMaterialIndex = 0;
    [SerializeField] private Material screenNormalMaterial;
    [SerializeField] private Material screenDeadMaterial;

    [Header("Narrative/Objective Lock")]
    [SerializeField] private ObjectiveSO requiredObjective;
    [SerializeField] private DialogueSO lockedDialogue;

    [Header("Dialogue & Prompt Setup")]
    [SerializeField] private DialogueSO deadPhoneDialogue;
    [SerializeField] private string promptMessage = "Check Phone";

    [Header("Narrative & Objective Progression")]
    [SerializeField] private ObjectiveSO objectiveToComplete;
    [SerializeField] private ObjectiveSO objectiveToActivate;

    [Header("Event Channels")]
    [SerializeField] private GameEventSO onPhoneDiedEvent;
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string phoneDiedInteractionID = "Call Police";

    [Header("Initial State")]
    [SerializeField] private bool isPhoneDeadOnStart = false;

    private Material[] sharedMaterialsCache;
    private bool isPhoneDead;

    public string PromptMessage => IsPhoneUnlocked() ? promptMessage : "";

    private void Awake()
    {
        if (phoneScreenRenderer != null)
        {
            sharedMaterialsCache = phoneScreenRenderer.sharedMaterials;
        }

        SetPhoneDeadState(isPhoneDeadOnStart);
    }

    private void OnEnable()
    {
        if (onPhoneDiedEvent != null)
        {
            onPhoneDiedEvent.RegisterListener(KillPhone);
        }

        if (interactionChannel != null)
        {
            interactionChannel.RegisterListener(HandleInteractionEvent);
        }
    }

    private void OnDisable()
    {
        if (onPhoneDiedEvent != null)
        {
            onPhoneDiedEvent.UnregisterListener(KillPhone);
        }

        if (interactionChannel != null)
        {
            interactionChannel.UnregisterListener(HandleInteractionEvent);
        }
    }

    public bool IsPhoneUnlocked()
    {
        if (!isPhoneDead) return false;

        if (requiredObjective == null) return true;

        if (ObjectiveManager.Instance != null)
        {
            return ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) == ObjectiveStatus.Active;
        }

        return false;
    }

    public void Interact()
    {
        if (!IsPhoneUnlocked())
        {
            if (lockedDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(lockedDialogue);
            }
            return;
        }

        if (ObjectiveManager.Instance != null)
        {
            if (objectiveToComplete != null)
            {
                ObjectiveManager.Instance.CompleteObjective(objectiveToComplete.ID);
            }

            if (objectiveToActivate != null)
            {
                ObjectiveManager.Instance.ActivateObjective(objectiveToActivate);
            }
        }

        if (DialogueManager.Instance != null && deadPhoneDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(deadPhoneDialogue);
        }
    }

    public void KillPhone()
    {
        SetPhoneDeadState(true);
    }

    private void HandleInteractionEvent(string eventID)
    {
        if (eventID == phoneDiedInteractionID)
        {
            KillPhone();
        }
    }

    public void SetPhoneDeadState(bool dead)
    {
        isPhoneDead = dead;

        if (phoneScreenRenderer != null && screenNormalMaterial != null && screenDeadMaterial != null && sharedMaterialsCache != null)
        {
            if (screenMaterialIndex >= 0 && screenMaterialIndex < sharedMaterialsCache.Length)
            {
                sharedMaterialsCache[screenMaterialIndex] = isPhoneDead ? screenDeadMaterial : screenNormalMaterial;
                phoneScreenRenderer.sharedMaterials = sharedMaterialsCache;
            }
        }
    }
}