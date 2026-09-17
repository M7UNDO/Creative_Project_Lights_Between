using UnityEngine;

public class ObjectGrabbable : MonoBehaviour
{
    private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;

    private RigidbodyConstraints originalConstraints;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();
        originalConstraints = objectRigidbody.constraints;
    }

    public void Grab(Transform objectGrabPointTransform)
    {
        this.objectGrabPointTransform = objectGrabPointTransform;

        objectRigidbody.useGravity = false;
        objectRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void Drop()
    {
        this.objectGrabPointTransform = null;

        objectRigidbody.useGravity = true;
        objectRigidbody.constraints = originalConstraints;
    }

    private void FixedUpdate()
    {
        if (objectGrabPointTransform != null)
        {
            float lerpSpeed = 10f;

            Vector3 newPosition = Vector3.Lerp(
                objectRigidbody.position,
                objectGrabPointTransform.position,
                Time.fixedDeltaTime * lerpSpeed
            );

            objectRigidbody.MovePosition(newPosition);
        }
    }
}