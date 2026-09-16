using System.Collections.Generic;
using UnityEngine;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] private GameObject objectivePrefab;
    [SerializeField] private Transform containerTransform;

    private readonly Dictionary<string, ObjectiveUIItem> spawnedItems = new Dictionary<string, ObjectiveUIItem>();

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
        if (objectiveDef == null) return;

        if (status == ObjectiveStatus.Active)
        {
            if (!spawnedItems.ContainsKey(objectiveDef.ID))
            {
                if (objectivePrefab == null || containerTransform == null)
                {
                    Debug.LogError("[ObjectiveUI] Missing objectivePrefab or containerTransform reference on " + gameObject.name);
                    return;
                }

                GameObject objInstance = Instantiate(objectivePrefab, containerTransform);
                ObjectiveUIItem item = objInstance.GetComponent<ObjectiveUIItem>();

                if (item != null)
                {
                    item.Setup(objectiveDef);
                    spawnedItems.Add(objectiveDef.ID, item);
                }
                else
                {
                    Debug.LogError("[ObjectiveUI] The assigned objectivePrefab is missing the ObjectiveUIItem component!");
                }
            }
        }
        else if (status == ObjectiveStatus.Completed)
        {
            if (spawnedItems.TryGetValue(objectiveDef.ID, out ObjectiveUIItem item))
            {
                item.SetCompleted(true);
            }
        }
    }
}