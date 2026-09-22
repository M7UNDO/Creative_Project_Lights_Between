using System.Collections;
using UnityEngine;

public class ElectricBoxDoorTrigger : MonoBehaviour
{
    [Header("Door & Breaker References")]
    [SerializeField] private RotatingOpenable doorOpenable;
    [SerializeField] private ElectricBoxBreaker breakerBox;

    [Header("Dialogue Asset")]
    [SerializeField] private DialogueSO basementDialogue;

    [Header("Door State Rules")]
    [SerializeField] private bool triggerOnce = true;

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

        if (DialogueManager.Instance != null && basementDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(basementDialogue);
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