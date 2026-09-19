using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PizzaEatingPlate : MonoBehaviour
{
    [Header("Slice Tracking")]
    [SerializeField] private List<PizzaSlice> pizzaSlices = new List<PizzaSlice>();
    [SerializeField] private float eatDelayDuration = 3.0f;

    [Header("Events & Audio")]
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string interactionEventID = "DinnerEaten";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chewingSFX;

    private int slicesEatenCount = 0;
    private int totalSlices = 0;
    private bool isEatingSlice;
    private bool isFullyEaten;

    private void Awake()
    {
        if (pizzaSlices == null || pizzaSlices.Count == 0)
        {
            pizzaSlices = new List<PizzaSlice>(GetComponentsInChildren<PizzaSlice>());
        }
        totalSlices = pizzaSlices.Count;
    }

    public bool CanEatSlice()
    {
        return !isEatingSlice && !isFullyEaten;
    }

    public void EatSlice(PizzaSlice slice)
    {
        if (!CanEatSlice() || slice == null) return;

        StartCoroutine(EatSliceRoutine(slice));
    }

    private IEnumerator EatSliceRoutine(PizzaSlice slice)
    {
        isEatingSlice = true;

        if (audioSource != null && chewingSFX != null)
        {
            audioSource.PlayOneShot(chewingSFX);
        }

        if (pizzaSlices.Contains(slice))
        {
            pizzaSlices.Remove(slice);
        }

        Destroy(slice.gameObject);
        slicesEatenCount++;

        yield return new WaitForSeconds(eatDelayDuration);

        isEatingSlice = false;

        if (pizzaSlices.Count == 0 || slicesEatenCount >= totalSlices)
        {
            isFullyEaten = true;

            if (interactionChannel != null)
            {
                interactionChannel.Raise(interactionEventID);
            }
        }
    }
}