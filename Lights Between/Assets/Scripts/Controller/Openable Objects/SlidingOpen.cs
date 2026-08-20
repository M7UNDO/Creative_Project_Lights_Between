using System.Collections;
using UnityEngine;

public class SlidingOpenable : BaseOpenable
{
    [Header("Sliding Configuration")]
    [SerializeField] private Vector3 closedLocalPosition;
    [SerializeField] private Vector3 openLocalPosition;

    protected override void Awake()
    {
        base.Awake();
        transform.localPosition = isOpen ? openLocalPosition : closedLocalPosition;
    }

    protected override IEnumerator AnimateTransition()
    {
        Vector3 targetPosition = isOpen ? openLocalPosition : closedLocalPosition;

        while (Vector3.Distance(transform.localPosition, targetPosition) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                Time.deltaTime * 5f * smoothSpeed
            );
            yield return null;
        }

        transform.localPosition = targetPosition;
    }
}