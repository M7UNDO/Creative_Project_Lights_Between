using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ObjectGrabbable : MonoBehaviour
{
    [Header("Grab Physics Settings")]
    [SerializeField] private float followSpeed = 20f;
    [SerializeField] private float rotateSpeed = 20f;
    [SerializeField] private float maxVelocity = 10f;
    [SerializeField] private float breakDistance = 2.5f;

    private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;
    private int originalLayer;
    private System.Action onForceDrop;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();

        objectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        objectRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Grab(Transform grabPoint, int heldLayer, System.Action forceDropCallback = null)
    {
        objectGrabPointTransform = grabPoint;
        onForceDrop = forceDropCallback;

        originalLayer = gameObject.layer;
        gameObject.layer = heldLayer;

        objectRigidbody.useGravity = false;
        objectRigidbody.linearVelocity = Vector3.zero;
        objectRigidbody.angularVelocity = Vector3.zero;
    }

    public void Drop()
    {
        if (objectGrabPointTransform == null) return;

        gameObject.layer = originalLayer;
        objectGrabPointTransform = null;
        onForceDrop = null;

        objectRigidbody.useGravity = true;

        objectRigidbody.linearVelocity = Vector3.ClampMagnitude(objectRigidbody.linearVelocity, maxVelocity * 0.5f);
    }

    private void FixedUpdate()
    {
        if (objectGrabPointTransform != null)
        {
            Vector3 distanceToTarget = objectGrabPointTransform.position - transform.position;

            if (distanceToTarget.sqrMagnitude > breakDistance * breakDistance)
            {
                onForceDrop?.Invoke();
                Drop();
                return;
            }

            Vector3 targetVelocity = distanceToTarget * followSpeed;
            objectRigidbody.linearVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);

            Quaternion targetRotation = objectGrabPointTransform.rotation;
            objectRigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotateSpeed));
        }
    }
}