using System.Collections;
using UnityEngine;

public class SlidingOpenable : BaseOpenable
{
    [Header("Sliding Configuration")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform[] additionalTransforms;
    [SerializeField] private Vector3 openLocalPosition;

    private Vector3 closedLocalPosition;
    private Vector3[] additionalClosedPositions;
    private Vector3[] additionalOpenPositions;

    protected override void Awake()
    {
        base.Awake();

        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        closedLocalPosition = targetTransform.localPosition;
        Vector3 offsetDelta = openLocalPosition - closedLocalPosition;

        InitializeAdditionalTransforms(offsetDelta);

        ApplyPositionState(isOpen);
    }

    protected override IEnumerator AnimateTransition()
    {
        Vector3 startPosition = targetTransform.localPosition;
        Vector3 targetPosition = isOpen ? openLocalPosition : closedLocalPosition;

        Vector3[] startAdditionalPositions = GetCurrentAdditionalPositions();
        Vector3[] targetAdditionalPositions = isOpen ? additionalOpenPositions : additionalClosedPositions;

        float duration = 1f / Mathf.Max(0.001f, 5f * smoothSpeed);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            targetTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            AnimateAdditionalTransforms(startAdditionalPositions, targetAdditionalPositions, t);

            yield return null;
        }

        targetTransform.localPosition = targetPosition;
        SetFinalAdditionalPositions(targetAdditionalPositions);
    }

    private void InitializeAdditionalTransforms(Vector3 offsetDelta)
    {
        if (additionalTransforms == null || additionalTransforms.Length == 0)
        {
            return;
        }

        additionalClosedPositions = new Vector3[additionalTransforms.Length];
        additionalOpenPositions = new Vector3[additionalTransforms.Length];

        for (int i = 0; i < additionalTransforms.Length; i++)
        {
            if (additionalTransforms[i] != null)
            {
                additionalClosedPositions[i] = additionalTransforms[i].localPosition;
                additionalOpenPositions[i] = additionalTransforms[i].localPosition + offsetDelta;
            }
        }
    }

    private void ApplyPositionState(bool targetIsOpen)
    {
        targetTransform.localPosition = targetIsOpen ? openLocalPosition : closedLocalPosition;

        if (additionalTransforms == null)
        {
            return;
        }

        for (int i = 0; i < additionalTransforms.Length; i++)
        {
            if (additionalTransforms[i] != null)
            {
                additionalTransforms[i].localPosition = targetIsOpen ? additionalOpenPositions[i] : additionalClosedPositions[i];
            }
        }
    }

    private Vector3[] GetCurrentAdditionalPositions()
    {
        if (additionalTransforms == null)
        {
            return new Vector3[0];
        }

        Vector3[] positions = new Vector3[additionalTransforms.Length];
        for (int i = 0; i < additionalTransforms.Length; i++)
        {
            if (additionalTransforms[i] != null)
            {
                positions[i] = additionalTransforms[i].localPosition;
            }
        }

        return positions;
    }

    private void AnimateAdditionalTransforms(Vector3[] startPositions, Vector3[] targetPositions, float t)
    {
        if (additionalTransforms == null)
        {
            return;
        }

        for (int i = 0; i < additionalTransforms.Length; i++)
        {
            if (additionalTransforms[i] != null)
            {
                additionalTransforms[i].localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            }
        }
    }

    private void SetFinalAdditionalPositions(Vector3[] targetPositions)
    {
        if (additionalTransforms == null)
        {
            return;
        }

        for (int i = 0; i < additionalTransforms.Length; i++)
        {
            if (additionalTransforms[i] != null)
            {
                additionalTransforms[i].localPosition = targetPositions[i];
            }
        }
    }
}