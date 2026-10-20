using UnityEngine;

public class PlayerPhoneInteractable : MonoBehaviour, IInteractable
{
    [Header("Screen Material References")]
    [SerializeField] private MeshRenderer phoneScreenRenderer;
    [SerializeField] private int screenMaterialIndex = 0;
    [SerializeField] private Material screenNormalMaterial;
    [SerializeField] private Material screenDeadMaterial;

    [Header("Dialogue Setup")]
    [SerializeField] private DialogueSO deadPhoneDialogue;
    [SerializeField] private string promptMessage = "Check Phone";

    [Header("Event Channels")]
    [SerializeField] private GameEventSO onPhoneDiedEvent;
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string phoneDiedInteractionID = "Call Police";

    [Header("Initial State")]
    [SerializeField] private bool isPhoneDeadOnStart = false;

    private Material[] sharedMaterialsCache;
    private bool isPhoneDead;

    public string PromptMessage => isPhoneDead ? promptMessage : "";

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

    public void Interact()
    {
        if (!isPhoneDead) return;

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