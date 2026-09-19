using UnityEngine;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] private ObjectiveUIItem objectiveItem;

    private void Start()
    {
        SubscribeAndSync();
    }

    private void OnEnable()
    {
        SubscribeAndSync();
    }

    private void OnDisable()
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
        }
    }

    private void SubscribeAndSync()
    {
        if (ObjectiveManager.Instance == null) return;

        ObjectiveManager.Instance.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
        ObjectiveManager.Instance.OnObjectiveStateChanged += HandleObjectiveStateChanged;

        foreach (var kvp in ObjectiveManager.Instance.RuntimeStates)
        {
            ObjectiveSO def = ObjectiveManager.Instance.GetObjectiveDefinition(kvp.Key);

            if (def != null)
            {
                HandleObjectiveStateChanged(def, kvp.Value);
            }
        }
    }

    private void HandleObjectiveStateChanged(ObjectiveSO objectiveDef, ObjectiveStatus status)
    {
        if (objectiveDef == null || objectiveItem == null) return;

        if (status == ObjectiveStatus.Active)
        {
            objectiveItem.Setup(objectiveDef);
        }
        else if (status == ObjectiveStatus.Completed)
        {
            objectiveItem.SetCompleted(true);
        }
    }
}