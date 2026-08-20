using System.Collections;
using UnityEngine;

public class RotatingOpenable : BaseOpenable
{
    [Header("Rotation Configuration")]
    [SerializeField] private Vector3 closedLocalRotationEuler;
    [SerializeField] private Vector3 openLocalRotationEuler;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    protected override void Awake()
    {
        base.Awake();
        closedRotation = Quaternion.Euler(closedLocalRotationEuler);
        openRotation = Quaternion.Euler(openLocalRotationEuler);
        transform.localRotation = isOpen ? openRotation : closedRotation;
    }

    protected override IEnumerator AnimateTransition()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;

        while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * 5f * smoothSpeed
            );
            yield return null;
        }

        transform.localRotation = targetRotation;
    }
}