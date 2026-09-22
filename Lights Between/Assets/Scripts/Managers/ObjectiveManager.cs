using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Event Channels")]
    [SerializeField] private StringGameEventSO interactionChannel;

    [Header("Story Progression")]
    [SerializeField] private StoryBeatSO initialStoryBeat;

    public event Action<ObjectiveSO, ObjectiveStatus> OnObjectiveStateChanged;
    public event Action<StoryBeatSO> OnStoryBeatStarted;
    public event Action<StoryBeatSO> OnStoryBeatCompleted;

    private StoryBeatSO currentBeat;
    private readonly Dictionary<string, ObjectiveSO> objectiveDatabase = new Dictionary<string, ObjectiveSO>();
    private readonly Dictionary<string, ObjectiveStatus> runtimeStates = new Dictionary<string, ObjectiveStatus>();
    private readonly List<ObjectiveSO> currentActiveObjectives = new List<ObjectiveSO>();

    public StoryBeatSO CurrentBeat => currentBeat;
    public IReadOnlyDictionary<string, ObjectiveStatus> RuntimeStates => runtimeStates;

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
        if (initialStoryBeat != null)
        {
            SetStoryBeat(initialStoryBeat);
        }
    }

    private void OnEnable()
    {
        if (interactionChannel != null)
        {
            interactionChannel.RegisterListener(HandleInteractionEvent);
        }
    }

    private void OnDisable()
    {
        if (interactionChannel != null)
        {
            interactionChannel.UnregisterListener(HandleInteractionEvent);
        }
    }

    public ObjectiveSO GetObjectiveDefinition(string id)
    {
        if (objectiveDatabase.TryGetValue(id, out ObjectiveSO def))
        {
            return def;
        }
        return null;
    }

    public ObjectiveStatus GetObjectiveStatus(string objectiveID)
    {
        if (runtimeStates.TryGetValue(objectiveID, out ObjectiveStatus status))
        {
            return status;
        }
        return ObjectiveStatus.Inactive;
    }

    public void SetStoryBeat(StoryBeatSO newBeat)
    {
        if (newBeat == null) return;

        currentBeat = newBeat;

        var beatObjectives = currentBeat.ActiveObjectives;
        for (int i = 0; i < beatObjectives.Count; i++)
        {
            ObjectiveSO obj = beatObjectives[i];
            if (obj != null)
            {
                ActivateObjective(obj);
            }
        }

        OnStoryBeatStarted?.Invoke(currentBeat);
    }

    public void ActivateObjective(ObjectiveSO objectiveDef)
    {
        if (objectiveDef == null) return;

        if (!objectiveDatabase.ContainsKey(objectiveDef.ID))
        {
            objectiveDatabase.Add(objectiveDef.ID, objectiveDef);
            runtimeStates.Add(objectiveDef.ID, ObjectiveStatus.Inactive);
        }

        if (runtimeStates[objectiveDef.ID] == ObjectiveStatus.Active) return;

        runtimeStates[objectiveDef.ID] = ObjectiveStatus.Active;

        if (!currentActiveObjectives.Contains(objectiveDef))
        {
            currentActiveObjectives.Add(objectiveDef);
        }

        OnObjectiveStateChanged?.Invoke(objectiveDef, ObjectiveStatus.Active);
    }

    public void CompleteObjective(string objectiveID)
    {
        if (!objectiveDatabase.TryGetValue(objectiveID, out ObjectiveSO def)) return;

        if (runtimeStates[objectiveID] == ObjectiveStatus.Completed) return;

        runtimeStates[objectiveID] = ObjectiveStatus.Completed;
        currentActiveObjectives.Remove(def);

        OnObjectiveStateChanged?.Invoke(def, ObjectiveStatus.Completed);

        CheckBeatCompletion();
    }

    private void HandleInteractionEvent(string interactionID)
    {
        for (int i = currentActiveObjectives.Count - 1; i >= 0; i--)
        {
            ObjectiveSO obj = currentActiveObjectives[i];
            if (obj != null && obj.TargetInteractionID == interactionID)
            {
                CompleteObjective(obj.ID);
            }
        }
    }

    private void CheckBeatCompletion()
    {
        if (currentBeat == null || currentActiveObjectives.Count > 0) return;

        OnStoryBeatCompleted?.Invoke(currentBeat);

        if (currentBeat.CompletionStoryEvent != null)
        {
            currentBeat.CompletionStoryEvent.Raise();
        }

        if (currentBeat.NextBeat != null)
        {
            SetStoryBeat(currentBeat.NextBeat);
        }
    }
}