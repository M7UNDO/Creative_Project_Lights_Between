using UnityEngine;

public class PizzaSliceEat : MonoBehaviour, IInteractable
{
    [SerializeField] private HoldInteraction holdInteraction;

    public void Interact()
    { 
       Destroy(gameObject);
    }
}
