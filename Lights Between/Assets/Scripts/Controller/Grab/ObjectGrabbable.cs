using UnityEngine;

public class ObjectGrabbable : MonoBehaviour
{
    [Header("Grab Offset")]
    [SerializeField] private bool useCustomGrabOffset = false;
    [SerializeField] private Vector3 grabPositionOffset;
    [SerializeField] private Vector3 grabRotationOffset;

    private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;

    private RigidbodyConstraints originalConstraints;

    private Vector3 defaultHoldPointPosition;
    private Quaternion defaultHoldPointRotation;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();
        originalConstraints = objectRigidbody.constraints;
    }

    public void Grab(Transform objectGrabPointTransform)
    {
        this.objectGrabPointTransform = objectGrabPointTransform;

        if (this.objectGrabPointTransform != null)
        {
            defaultHoldPointPosition = this.objectGrabPointTransform.localPosition;
            defaultHoldPointRotation = this.objectGrabPointTransform.localRotation;

            if (useCustomGrabOffset)
            {
                this.objectGrabPointTransform.localPosition = grabPositionOffset;
                this.objectGrabPointTransform.localRotation = Quaternion.Euler(grabRotationOffset);
            }
        }

        objectRigidbody.useGravity = false;
        objectRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void Drop()
    {
        if (objectGrabPointTransform != null)
        {
            objectGrabPointTransform.localPosition = defaultHoldPointPosition;
            objectGrabPointTransform.localRotation = defaultHoldPointRotation;
        }

        objectGrabPointTransform = null;

        objectRigidbody.useGravity = true;
        objectRigidbody.constraints = originalConstraints;
    }

    private void FixedUpdate()
    {
        if (objectGrabPointTransform != null)
        {
            float lerpSpeed = 10f;

            Vector3 targetPosition = objectGrabPointTransform.position;
            Quaternion targetRotation = objectGrabPointTransform.rotation;

            Vector3 newPosition = Vector3.Lerp(
                objectRigidbody.position,
                targetPosition,
                Time.fixedDeltaTime * lerpSpeed
            );

            objectRigidbody.MovePosition(newPosition);
            objectRigidbody.MoveRotation(targetRotation);
        }
    }
}