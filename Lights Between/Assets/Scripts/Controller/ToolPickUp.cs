using UnityEngine;

public class ToolPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ToolClass tool;
    [SerializeField] private string promptMessage = "Pick up";

    [Header("Narrative/Objective Lock")]
    [Tooltip("If assigned, the player can only interact when this objective is active.")]
    [SerializeField] private ObjectiveSO requiredObjective;
    [Tooltip("Optional dialogue to play if player tries to use this before it's unlocked.")]
    [SerializeField] private DialogueSO lockedDialogue;

    [Header("Narrative & Objective Progression")]
    [SerializeField] private ObjectiveSO objectiveToComplete;
    [SerializeField] private ObjectiveSO objectiveToActivate;
    [SerializeField] private DialogueSO postPickupDialogue;

    [Header("SFX")]
    [SerializeField] private AudioClip pickUpSFX;

    public string PromptMessage => promptMessage;

    public bool IsPickupUnlocked()
    {
        if (requiredObjective == null) return true;

        if (ObjectiveManager.Instance != null)
        {
            return ObjectiveManager.Instance.GetObjectiveStatus(requiredObjective.ID) == ObjectiveStatus.Active;
        }

        return false;
    }

    public void Interact()
    {
        if (!IsPickupUnlocked())
        {
            if (lockedDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(lockedDialogue);
            }
            return;
        }

        PlayerToolInventory inventory = FindFirstObjectByType<PlayerToolInventory>();

        if (inventory != null)
        {
            inventory.AddTool(tool);

            if (pickUpSFX != null)
            {
                AudioSource.PlayClipAtPoint(pickUpSFX, transform.position);
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

            if (DialogueManager.Instance != null && postPickupDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(postPickupDialogue);
            }

            ToolEquipmentManager equipmentManager = FindFirstObjectByType<ToolEquipmentManager>();
            if (equipmentManager != null && !equipmentManager.HasToolEquipped)
            {
                equipmentManager.EquipTool(tool);
            }

            Destroy(gameObject);
        }
    }
}