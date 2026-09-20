using UnityEngine;

public class PizzaSlice : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptMessage = "Eat Slice";

    [Header("Monologue References")]
    [SerializeField] private DialogueSO mustSitDialogue;      // "I should sit down to eat..."
    [SerializeField] private DialogueSO turnOnTVDialogue;     // "I wanted to watch some TV while eating..."

    [Header("TV Reference")]
    [SerializeField] private TVController tvController;

    private PizzaEatingPlate parentPlate;

    private void Awake()
    {
        parentPlate = GetComponentInParent<PizzaEatingPlate>();

        if (tvController == null)
        {
            tvController = FindFirstObjectByType<TVController>();
        }
    }

    public void Interact()
    {
        FPSController player = FindFirstObjectByType<FPSController>();

        if (player != null && !player.isSeated)
        {
            if (DialogueManager.Instance != null && mustSitDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(mustSitDialogue);
            }
            return;
        }

        if (tvController != null && !tvController.IsTVOn)
        {
            if (DialogueManager.Instance != null && turnOnTVDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(turnOnTVDialogue);
            }
            return;
        }

        // Both conditions met -> Eat slice
        if (parentPlate != null && CanInteract())
        {
            parentPlate.EatSlice(this);
        }
    }

    public string GetPromptMessage()
    {
        return promptMessage;
    }

    public bool CanInteract()
    {
        FPSController player = FindFirstObjectByType<FPSController>();
        bool playerIsSeated = player != null && player.isSeated;
        bool isTvOn = tvController != null && tvController.IsTVOn;

        return playerIsSeated && isTvOn && parentPlate != null && parentPlate.CanEatSlice();
    }
}