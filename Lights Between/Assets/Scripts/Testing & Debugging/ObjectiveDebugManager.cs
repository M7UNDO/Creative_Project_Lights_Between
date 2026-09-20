using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectiveDebugManager : MonoBehaviour
{
    [Header("Override Settings")]
    [Tooltip("Enable this to override the default story beat / objective start state.")]
    [SerializeField] private bool overrideOnStart = false;

    [Tooltip("The objective you want to force-activate when starting the scene.")]
    [SerializeField] private ObjectiveSO startObjectiveOverride;

    [Header("New Input System Debug Controls")]
    [Tooltip("Input Action to trigger completion of the active override objective (e.g., press F1 or a gamepad button).")]
    [SerializeField] private InputActionReference completeObjectiveAction;

    private void OnEnable()
    {
        if (completeObjectiveAction != null)
        {
            completeObjectiveAction.action.Enable();
            completeObjectiveAction.action.performed += OnCompleteObjectivePerformed;
        }
    }

    private void OnDisable()
    {
        if (completeObjectiveAction != null)
        {
            completeObjectiveAction.action.performed -= OnCompleteObjectivePerformed;
            completeObjectiveAction.action.Disable();
        }
    }

    private void Start()
    {
        if (overrideOnStart && startObjectiveOverride != null)
        {
            ForceStartObjective(startObjectiveOverride);
        }
    }

    private void OnCompleteObjectivePerformed(InputAction.CallbackContext context)
    {
        CompleteCurrentDebugObjective();
    }

    public void ForceStartObjective(ObjectiveSO objective)
    {
        if (objective == null) return;

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ActivateObjective(objective);
            Debug.Log($"<color=yellow>[DEBUG]</color> Force Activated Objective: {objective.Title} ({objective.ID})");
        }
        else
        {
            Debug.LogWarning("[DEBUG] ObjectiveManager Instance not found in scene!");
        }
    }

    public void CompleteCurrentDebugObjective()
    {
        if (startObjectiveOverride != null && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.CompleteObjective(startObjectiveOverride.ID);
            Debug.Log($"<color=green>[DEBUG]</color> Force Completed Objective: {startObjectiveOverride.Title} ({startObjectiveOverride.ID})");
        }
    }
}