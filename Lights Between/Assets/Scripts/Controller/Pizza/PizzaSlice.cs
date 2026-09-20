using UnityEngine;

public class PizzaSlice : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptMessage = "Eat Slice";
    [SerializeField] private DialogueSO mustSitDialogue;

    private PizzaEatingPlate parentPlate;

    private void Awake()
    {
        parentPlate = GetComponentInParent<PizzaEatingPlate>();
    }

    public void Interact()
    {
        FPSController player = FindFirstObjectByType<FPSController>();

        if (player != null && !player.isSeated)
        {
            // Play Miguel's internal monologue if he tries to eat standing up
            if (DialogueManager.Instance != null && mustSitDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(mustSitDialogue);
            }
            return;
        }

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

        return playerIsSeated && parentPlate != null && parentPlate.CanEatSlice();
    }
}