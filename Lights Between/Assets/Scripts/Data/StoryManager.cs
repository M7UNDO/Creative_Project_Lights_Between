using System;
using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [SerializeField] private StoryBeatSO initialStoryBeat;

    public event Action<StoryBeatSO> OnStoryBeatStarted;
    public event Action<StoryBeatSO> OnStoryBeatCompleted;

    public StoryBeatSO CurrentBeat { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveStateChanged += HandleObjectiveStateChanged;
        }

        if (initialStoryBeat != null)
        {
            StartStoryBeat(initialStoryBeat);
        }
    }

    private void OnDestroy()
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
        }
    }

    public void StartStoryBeat(StoryBeatSO beat)
    {
        if (beat == null) return;

        CurrentBeat = beat;
        OnStoryBeatStarted?.Invoke(CurrentBeat);

        // REMOVED: Automatic ActivateObjective() foreach loop.
        // Objectives are now activated organically by scene triggers, notes, and dialogues!
    }

    private void HandleObjectiveStateChanged(ObjectiveSO objectiveDef, ObjectiveStatus status)
    {
        if (status == ObjectiveStatus.Completed)
        {
            CheckBeatCompletion();
        }
    }

    private void CheckBeatCompletion()
    {
        if (CurrentBeat == null || ObjectiveManager.Instance == null) return;

        bool allCompleted = true;

        foreach (ObjectiveSO obj in CurrentBeat.ActiveObjectives)
        {
            if (ObjectiveManager.Instance.GetObjectiveStatus(obj.ID) != ObjectiveStatus.Completed)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted && CurrentBeat.ActiveObjectives.Count > 0)
        {
            CompleteCurrentStoryBeat();
        }
    }

    private void CompleteCurrentStoryBeat()
    {
        StoryBeatSO completedBeat = CurrentBeat;
        OnStoryBeatCompleted?.Invoke(completedBeat);

        if (completedBeat.CompletionStoryEvent != null)
        {
            completedBeat.CompletionStoryEvent.Raise();
        }

        if (completedBeat.NextBeat != null)
        {
            StartStoryBeat(completedBeat.NextBeat);
        }
    }
}