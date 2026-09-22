using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JournalPowerOutageTrigger : MonoBehaviour, IInteractable
{
    [Header("Journal Data")]
    [SerializeField] private NoteData journalData;
    [SerializeField] private string promptMessageText = "Read Dad's Journal";

    [Header("Target Breaker Box")]
    [SerializeField] private ElectricBoxBreaker breakerBox;

    [Header("Post-Journal Triggers To Activate")]
    [Tooltip("Drag the Exit Trigger Zone GameObject here.")]
    [SerializeField] private List<GameObject> triggersToActivate = new List<GameObject>();

    private bool hasTriggeredPowerOutage = false;

    public string promptMessage => promptMessageText;

    public bool CanInteract()
    {
        return journalData != null;
    }

    public void Interact()
    {
        if (journalData == null) return;

        if (NoteUIController.Instance != null)
        {
            NoteUIController.Instance.OpenNote(journalData);
            StartCoroutine(WaitForNoteClose());
        }
    }

    private IEnumerator WaitForNoteClose()
    {
        yield return new WaitUntil(() => !NoteUIController.isNoteOpen);

        if (!hasTriggeredPowerOutage)
        {
            hasTriggeredPowerOutage = true;

            if (breakerBox != null)
            {
                breakerBox.TripBreaker();
                breakerBox.SetLeverMissingState(true);
            }

            if (triggersToActivate != null)
            {
                for (int i = 0; i < triggersToActivate.Count; i++)
                {
                    if (triggersToActivate[i] != null)
                    {
                        triggersToActivate[i].SetActive(true);
                    }
                }
            }
        }
    }
}