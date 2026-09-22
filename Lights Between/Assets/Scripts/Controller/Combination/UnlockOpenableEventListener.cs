using UnityEngine;

public class UnlockOpenableEventListener : MonoBehaviour
{
    [Header("Event Channel")]
    [SerializeField] private GameEventSO onLockUnlockedEvent;

    [Header("Target Openable")]
    [SerializeField] private BaseOpenable targetOpenable;

    private void OnEnable()
    {
        if (onLockUnlockedEvent != null)
        {
            onLockUnlockedEvent.RegisterListener(HandleLockUnlocked);
        }
    }

    private void OnDisable()
    {
        if (onLockUnlockedEvent != null)
        {
            onLockUnlockedEvent.UnregisterListener(HandleLockUnlocked);
        }
    }

    private void HandleLockUnlocked()
    {
        if (targetOpenable == null)
        {
            targetOpenable = GetComponent<BaseOpenable>();
        }

        if (targetOpenable != null)
        {
            targetOpenable.SetLockedState(false);
            Debug.Log($"[LockableListener] Successfully unlocked {targetOpenable.name}");
        }
    }
}