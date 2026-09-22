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
    [Tooltip("Input Action to trigger completion of the active override objective (e.g., F1).")]
    [SerializeField] private InputActionReference completeObjectiveAction;

    [Tooltip("Input Action to trigger a specific game event for testing (e.g., F2).")]
    [SerializeField] private InputActionReference raiseGameEventAction;

    [Header("Debug Game Event Cheat")]
    [Tooltip("The parameterless GameEventSO you want to raise via keypress.")]
    [SerializeField] private GameEventSO debugGameEventToRaise;

    [Header("Optional String Game Event Cheat")]
    [Tooltip("The StringGameEventSO channel to raise.")]
    [SerializeField] private StringGameEventSO debugStringGameEventChannel;
    [Tooltip("The event ID payload string to pass.")]
    [SerializeField] private string debugStringEventID = "EnterGarage";

    private void OnEnable()
    {
        if (completeObjectiveAction != null)
        {
            completeObjectiveAction.action.Enable();
            completeObjectiveAction.action.performed += OnCompleteObjectivePerformed;
        }

        if (raiseGameEventAction != null)
        {
            raiseGameEventAction.action.Enable();
            raiseGameEventAction.action.performed += OnRaiseGameEventPerformed;
        }
    }

    private void OnDisable()
    {
        if (completeObjectiveAction != null)
        {
            completeObjectiveAction.action.performed -= OnCompleteObjectivePerformed;
            completeObjectiveAction.action.Disable();
        }

        if (raiseGameEventAction != null)
        {
            raiseGameEventAction.action.performed -= OnRaiseGameEventPerformed;
            raiseGameEventAction.action.Disable();
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

    private void OnRaiseGameEventPerformed(InputAction.CallbackContext context)
    {
        TriggerDebugGameEvent();
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

    public void TriggerDebugGameEvent()
    {
        if (debugGameEventToRaise != null)
        {
            debugGameEventToRaise.Raise();
            Debug.Log($"<color=cyan>[DEBUG CHEAT]</color> Raised GameEventSO: {debugGameEventToRaise.name}");
        }

        if (debugStringGameEventChannel != null && !string.IsNullOrEmpty(debugStringEventID))
        {
            debugStringGameEventChannel.Raise(debugStringEventID);
            Debug.Log($"<color=cyan>[DEBUG CHEAT]</color> Raised StringGameEventSO ({debugStringGameEventChannel.name}) -> Payload: '{debugStringEventID}'");
        }
    }
}