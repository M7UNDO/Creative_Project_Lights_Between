using UnityEngine;

public class PizzaSlice : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptMessage = "Eat Slice";

    private PizzaEatingPlate parentPlate;

    private void Awake()
    {
        parentPlate = GetComponentInParent<PizzaEatingPlate>();
    }

    public void Interact()
    {
        if (parentPlate != null)
        {
            parentPlate.EatSlice(this);
        }
    }

    public string PromptMessage => promptMessage;

    public bool CanInteract()
    {
        return parentPlate != null && parentPlate.CanEatSlice();
    }
}