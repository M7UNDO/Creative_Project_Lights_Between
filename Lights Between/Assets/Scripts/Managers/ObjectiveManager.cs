using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [SerializeField] private StringGameEventSO interactionChannel;

    public event Action<ObjectiveSO, ObjectiveStatus> OnObjectiveStateChanged;

    private readonly Dictionary<string, ObjectiveSO> objectiveDatabase = new Dictionary<string, ObjectiveSO>();
    private readonly Dictionary<string, ObjectiveStatus> runtimeStates = new Dictionary<string, ObjectiveStatus>();


    public IReadOnlyDictionary<string, ObjectiveStatus> RuntimeStates => runtimeStates;

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
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    public void ActivateObjective(ObjectiveSO objectiveDef)
    {
        if (objectiveDef == null) return;

        if (!objectiveDatabase.ContainsKey(objectiveDef.ID))
        {
            objectiveDatabase.Add(objectiveDef.ID, objectiveDef);
            runtimeStates.Add(objectiveDef.ID, ObjectiveStatus.Inactive);
        }

        if (runtimeStates[objectiveDef.ID] != ObjectiveStatus.Inactive) return;

        runtimeStates[objectiveDef.ID] = ObjectiveStatus.Active;
        OnObjectiveStateChanged?.Invoke(objectiveDef, ObjectiveStatus.Active);
    }

    private void CompleteObjective(string objectiveID)
    {
        if (!objectiveDatabase.TryGetValue(objectiveID, out ObjectiveSO def)) return;

        if (runtimeStates[objectiveID] == ObjectiveStatus.Completed) return;

        runtimeStates[objectiveID] = ObjectiveStatus.Completed;
        OnObjectiveStateChanged?.Invoke(def, ObjectiveStatus.Completed);
    }

    private void HandleInteractionEvent(string interactionID)
    {
        List<string> activeKeys = new List<string>();

        foreach (var kvp in runtimeStates)
        {
            if (kvp.Value == ObjectiveStatus.Active)
            {
                activeKeys.Add(kvp.Key);
            }
        }

        for (int i = 0; i < activeKeys.Count; i++)
        {
            string key = activeKeys[i];
            if (objectiveDatabase.TryGetValue(key, out ObjectiveSO def))
            {
                if (def.TargetInteractionID == interactionID)
                {
                    CompleteObjective(def.ID);
                }
            }
        }
    }
}