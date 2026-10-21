using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JournalPowerOutageTrigger : MonoBehaviour, IInteractable
{
    [Header("Journal Data")]
    [SerializeField] private NoteData journalData;
    [SerializeField] private string promptMessageText = "Read Dad's Journal";

    [Header("Target Breaker Box")]
    [SerializeField] private ElectricBoxBreaker breakerBox;

    [Header("Enemy Activation Settings")]
    [SerializeField] private bool activateEnemyOnPowerOutage = true;
    [SerializeField] private EnemyAI.PatrolZone enemySpawnZone = EnemyAI.PatrolZone.Basement;

    [Header("Power Outage Events")]
    [SerializeField] private GameEventSO powerOutageEvent;
    [SerializeField] private UnityEvent onPowerOutage;

    [Header("Reaction Dialogue")]
    [TextArea(2, 4)]
    [SerializeField]
    private string[] dialogueBeforeOutage = new string[]
    {
        "They were so scared. They just wanted to get out of here.",
        "Dad was trying to protect us. He was trying to keep us safe.",
        "Oh God..."
    };

    [TextArea(2, 4)]
    [SerializeField]
    private string[] dialogueAfterOutage = new string[]
    {
        "No! The lights!",
        "It knows I'm here. It cut the power on purpose.",
        "I can't stay in the dark. I need to get to the basement. I have to get the power back on before it finds me.",
        "Please... please don't let it get me."
    };

    [SerializeField] private float delayAfterPowerCut = 0.3f;

    [Header("Objective Settings")]
    [SerializeField] private ObjectiveSO objectiveToActivate;

    [Header("Post-Journal Triggers To Activate")]
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

            if (DialogueManager.Instance != null && dialogueBeforeOutage != null && dialogueBeforeOutage.Length > 0)
            {
                DialogueManager.Instance.StartCustomLines(dialogueBeforeOutage, OnPreOutageDialogueComplete, managePlayerMode: true);
            }
            else
            {
                ExecutePowerOutage();
            }
        }
    }

    private void OnPreOutageDialogueComplete()
    {
        ExecutePowerOutage();
    }

    private void ExecutePowerOutage()
    {
        if (breakerBox != null)
        {
            breakerBox.TripBreaker();
            breakerBox.SetLeverMissingState(true);
        }

        if (activateEnemyOnPowerOutage && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ActivateEnemy(enemySpawnZone);
        }

        if (powerOutageEvent != null)
        {
            powerOutageEvent.Raise();
        }

        onPowerOutage?.Invoke();

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

        if (DialogueManager.Instance != null && dialogueAfterOutage != null && dialogueAfterOutage.Length > 0)
        {
            StartCoroutine(PlayPostOutageDialogueRoutine());
        }
        else
        {
            OnPostOutageDialogueComplete();
        }
    }

    private IEnumerator PlayPostOutageDialogueRoutine()
    {
        if (delayAfterPowerCut > 0f)
        {
            yield return new WaitForSeconds(delayAfterPowerCut);
        }

        DialogueManager.Instance.StartCustomLines(dialogueAfterOutage, OnPostOutageDialogueComplete, managePlayerMode: true);
    }

    private void OnPostOutageDialogueComplete()
    {
        if (objectiveToActivate != null && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ActivateObjective(objectiveToActivate);
        }
    }
}