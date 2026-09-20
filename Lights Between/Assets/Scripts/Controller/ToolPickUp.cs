using UnityEngine;

public class ToolPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ToolClass tool;
    [SerializeField] private string promptMessage = "Pick up";

    [Header("Narrative & Objective Progression")]
    [SerializeField] private ObjectiveSO objectiveToComplete; // Drag "Obj_FindTorch"
    [SerializeField] private ObjectiveSO objectiveToActivate; // Drag "Obj_RestorePower"
    [SerializeField] private DialogueSO postPickupDialogue;   // "Got it. Now I need to check the generator out back."

    [Header("SFX")]
    [SerializeField] private AudioClip pickUpSFX;

    public string PromptMessage => promptMessage;

    public void Interact()
    {
        PlayerToolInventory inventory = FindFirstObjectByType<PlayerToolInventory>();

        if (inventory != null)
        {
            inventory.AddTool(tool);

            if (pickUpSFX != null)
            {
                AudioSource.PlayClipAtPoint(pickUpSFX, transform.position);
            }

            ToolEquipmentManager equipmentManager = FindFirstObjectByType<ToolEquipmentManager>();
            if (equipmentManager != null && !equipmentManager.HasToolEquipped)
            {
                equipmentManager.EquipTool(tool);
            }

            // Progress Narrative Objectives
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

            // Trigger Reaction Dialogue
            if (DialogueManager.Instance != null && postPickupDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(postPickupDialogue);
            }

            Destroy(gameObject);
        }
    }
}