using UnityEngine;

public class TestObjectiveLoader : MonoBehaviour
{
    [SerializeField] private ObjectiveSO initialObjective;

    private void Start()
    {
        if (ObjectiveManager.Instance != null && initialObjective != null)
        {
            ObjectiveManager.Instance.ActivateObjective(initialObjective);
        }
    }
}