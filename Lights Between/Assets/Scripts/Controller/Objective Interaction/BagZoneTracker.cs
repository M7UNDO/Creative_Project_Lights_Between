using UnityEngine;

public class BagDropZoneTracker : MonoBehaviour
{
    [SerializeField] private StringGameEventSO interactionChannel;
    [SerializeField] private string bagTag = "PlayerBag";
    [SerializeField] private int totalBagsRequired = 2;

    private int currentBagCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(bagTag))
        {
            currentBagCount++;
            if (currentBagCount >= totalBagsRequired)
            {
                if (interactionChannel != null)
                {
                    interactionChannel.Raise(InteractionIDs.PackBags);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(bagTag))
        {
            currentBagCount = Mathf.Max(0, currentBagCount - 1);
        }
    }
}