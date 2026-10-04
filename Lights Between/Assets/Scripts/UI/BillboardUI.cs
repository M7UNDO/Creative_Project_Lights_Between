using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [SerializeField] private bool lockYRotation = false;

    private Transform mainCameraTransform;

    private void Awake()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (mainCameraTransform == null)
        {
            if (Camera.main == null) return;
            mainCameraTransform = Camera.main.transform;
        }

        Vector3 targetPosition = transform.position + mainCameraTransform.rotation * Vector3.forward;
        Vector3 targetOrientation = mainCameraTransform.rotation * Vector3.up;

        if (lockYRotation)
        {
            targetPosition.y = transform.position.y;
            targetOrientation = Vector3.up;
        }

        transform.LookAt(targetPosition, targetOrientation);
    }
}