using UnityEngine;
using UnityEngine.Events;

public class HoldInteraction : MonoBehaviour
{
    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private bool resetProgressWhenReleased = true;
    [SerializeField] private bool canRepeat = true;

    [Header("UI")]
    [SerializeField] private HoldProgressUI holdProgressUI;

    [Header("Events")]
    public UnityEvent onHoldStarted;
    public UnityEvent onHoldCancelled;
    public UnityEvent onHoldCompleted;

    private float holdTimer;
    private bool isHolding;
    private bool hasCompleted;

    public bool IsHolding
    {
        get { return isHolding; }
    }

    public float HoldProgress
    {
        get { return holdTimer / holdDuration; }
    }

    private void Update()
    {
        if (!isHolding) return;

        holdTimer += Time.deltaTime;

        if (holdProgressUI != null)
        {
            holdProgressUI.SetProgress(HoldProgress);
        }

        if (holdTimer >= holdDuration)
        {
            CompleteHold();
        }
    }

    public void BeginHold()
    {
        if (hasCompleted && !canRepeat) return;

        if (!isHolding)
        {
            isHolding = true;

            if (holdProgressUI != null)
            {
                holdProgressUI.Show();
                holdProgressUI.SetProgress(HoldProgress);
            }

            onHoldStarted?.Invoke();
        }
    }

    public void CancelHold()
    {
        if (!isHolding) return;

        isHolding = false;

        if (resetProgressWhenReleased)
        {
            holdTimer = 0f;
        }

        if (holdProgressUI != null)
        {
            holdProgressUI.Hide();
        }

        onHoldCancelled?.Invoke();
    }

    private void CompleteHold()
    {
        isHolding = false;
        hasCompleted = true;
        holdTimer = 0f;

        if (holdProgressUI != null)
        {
            holdProgressUI.Hide();
        }

        onHoldCompleted?.Invoke();
    }

    public void ResetHold()
    {
        holdTimer = 0f;
        hasCompleted = false;
        isHolding = false;

        if (holdProgressUI != null)
        {
            holdProgressUI.Hide();
        }
    }
}