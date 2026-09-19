using UnityEngine;

[RequireComponent(typeof(DialogueTrigger), typeof(Collider))]
public class DialogueTriggerZone : MonoBehaviour
{
    private DialogueTrigger dialogueTrigger;

    private void Awake()
    {
        dialogueTrigger = GetComponent<DialogueTrigger>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialogueTrigger.TriggerDialogue();
        }
    }
}