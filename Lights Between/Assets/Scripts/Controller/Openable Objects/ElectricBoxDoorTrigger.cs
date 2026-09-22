using System.Collections;
using UnityEngine;

public class ElectricBoxDoorTrigger : MonoBehaviour
{
    [Header("Door & Breaker References")]
    [SerializeField] private RotatingOpenable doorOpenable;
    [SerializeField] private ElectricBoxBreaker breakerBox;

    [Header("Dialogue Trigger")]
    [SerializeField] private DialogueTrigger basementDialogueTrigger;

    [Header("Door State Rules")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool autoCloseDoorOnPowerOutage = true;

    private bool hasTriggered = false;
    private bool wasOpen = false;

    private void Awake()
    {
        if (doorOpenable == null)
            doorOpenable = GetComponent<RotatingOpenable>();
    }

    private void Update()
    {
        if (doorOpenable == null || breakerBox == null) return;

        if (breakerBox.IsLeverMissing)
        {
            if (doorOpenable.isOpen && !wasOpen)
            {
                OnDoorOpenedDuringOutage();
            }
        }

        wasOpen = doorOpenable.isOpen;
    }

    private void OnDoorOpenedDuringOutage()
    {
        if (hasTriggered && triggerOnce) return;

        hasTriggered = true;

        if (basementDialogueTrigger != null)
        {
            basementDialogueTrigger.TriggerDialogue();
        }
    }

    public void ForceCloseDoor()
    {
        if (doorOpenable != null && doorOpenable.isOpen)
        {
            doorOpenable.Interact();
        }
    }
}