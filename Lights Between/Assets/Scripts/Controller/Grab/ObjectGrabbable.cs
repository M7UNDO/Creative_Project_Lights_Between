using UnityEngine;

public class ObjectGrabbable : MonoBehaviour
{
    [Header("Item Identification")]
    [SerializeField] private string itemID = "Bag";
    public string ItemID => itemID;

    [Header("Grab Offset")]
    [SerializeField] private bool useCustomGrabOffset = false;
    [SerializeField] private Vector3 grabPositionOffset;
    [SerializeField] private Vector3 grabRotationOffset;

    private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;
    private RigidbodyConstraints originalConstraints;

    private int originalLayer;
    private Collider[] objectColliders;

    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    [Header("SFX")]
    [SerializeField] private AudioClip pickUpSFX;

    private void Awake()
    {
        objectRigidbody = GetComponent<Rigidbody>();
        originalConstraints = objectRigidbody.constraints;
        originalLayer = gameObject.layer;
        objectColliders = GetComponentsInChildren<Collider>();
    }

    public void Grab(Transform objectGrabPointTransform, int targetLayer)
    {
        this.objectGrabPointTransform = objectGrabPointTransform;

        SetLayerRecursively(gameObject, targetLayer);

        if (pickUpSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickUpSFX, transform.position);
        }

        objectRigidbody.isKinematic = true;
        objectRigidbody.useGravity = false;

        foreach (Collider col in objectColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        // Parent the item to the player's anchor
        transform.SetParent(this.objectGrabPointTransform);

        // CLEAN ARCHITECTURE: Apply the offset to the item itself, NOT the player's anchor.
        if (useCustomGrabOffset)
        {
            transform.localPosition = grabPositionOffset;
            transform.localRotation = Quaternion.Euler(grabRotationOffset);
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        lastPosition = transform.position;
    }

    public void SnapToTarget(Transform snapTransform, bool destroyOnSnap = false)
    {
        transform.SetParent(null);
        SetLayerRecursively(gameObject, originalLayer);
        objectGrabPointTransform = null;

        if (destroyOnSnap)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = snapTransform.position;
        transform.rotation = snapTransform.rotation;

        objectRigidbody.isKinematic = true;
        objectRigidbody.useGravity = false;

        foreach (Collider col in objectColliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }

    public void Drop()
    {
        transform.SetParent(null);
        SetLayerRecursively(gameObject, originalLayer);
        objectGrabPointTransform = null;

        foreach (Collider col in objectColliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }

        objectRigidbody.isKinematic = false;
        objectRigidbody.useGravity = true;
        objectRigidbody.constraints = originalConstraints;

        objectRigidbody.linearVelocity = currentVelocity;
    }

    private void Update()
    {
        if (objectGrabPointTransform != null)
        {
            if (Time.deltaTime > 0f)
            {
                currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
                lastPosition = transform.position;
            }
        }
    }

    private void SetLayerRecursively(GameObject targetObject, int newLayer)
    {
        if (targetObject == null) return;

        targetObject.layer = newLayer;

        foreach (Transform child in targetObject.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}