using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class DoorScript : MonoBehaviour, IInteractable
{
    [Header("Door Movement")]
    public bool open;
    public float smooth = 0.5f;
    public float DoorOpenAngle = -90.0f;
    public float DoorCloseAngle = 0.0f;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string requiredKeyID = "";
    [SerializeField] private bool consumeKeyOnUse = true;

    [Header("Hold Interaction")]
    [SerializeField] private HoldInteraction holdInteraction;

    [Header("Audio")]
    public AudioSource asource;
    public AudioClip openDoor, closeDoor, lockedDoorSound, unlockDoorSound;

    [Header("Juice / Visual Feedback")]
    [SerializeField] private Transform doorMeshTransform;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 2.0f;

    public string promptMessage { get; private set; }

    public bool toggle;
    private Coroutine rotationCoroutine;
    private Coroutine shakeCoroutine;
    private NavMeshObstacle navObstacle;
    private IKeyInventory cachedInventory;

    public bool IsLocked => isLocked;

    private void Start()
    {
        asource = GetComponent<AudioSource>();
        navObstacle = GetComponent<NavMeshObstacle>();
        if (navObstacle == null)
        {
            navObstacle = GetComponentInChildren<NavMeshObstacle>();
        }

        if (doorMeshTransform == null)
        {
            doorMeshTransform = transform;
        }

        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.AddListener(OnUnlockHoldCompleted);
        }

        UpdatePromptMessage(null);
        UpdateNavObstacleState();
    }

    private void OnDestroy()
    {
        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.RemoveListener(OnUnlockHoldCompleted);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!open && other.CompareTag("Creature"))
        {
            ForceOpen();
        }
    }

    public bool CanInteract(IKeyInventory playerInventory = null)
    {
        if (!isLocked) return true;
        return playerInventory != null && playerInventory.HasKey(requiredKeyID);
    }

    public void UpdatePromptMessage(IKeyInventory playerInventory)
    {
        if (isLocked)
        {
            if (playerInventory != null && playerInventory.HasKey(requiredKeyID))
            {
                promptMessage = "Unlock Door";
            }
            else
            {
                promptMessage = "Locked";
            }
            return;
        }

        promptMessage = toggle ? "Close" : "Open";
    }

    public void Interact()
    {
        Interact(null);
    }

    public void Interact(IKeyInventory playerInventory)
    {
        cachedInventory = playerInventory;

        if (isLocked)
        {
            if (playerInventory != null && playerInventory.HasKey(requiredKeyID))
            {
                if (holdInteraction != null)
                {
                    if (!holdInteraction.IsHolding)
                    {
                        holdInteraction.BeginHold();
                    }
                }
                else
                {
                    Unlock(playerInventory);
                    ToggleDoorState(playerInventory);
                }
            }
            else
            {
                OnAttemptOpenLocked();
            }
            return;
        }

        ToggleDoorState(playerInventory);
    }

    public void CancelInteraction()
    {
        if (holdInteraction != null && holdInteraction.IsHolding)
        {
            holdInteraction.CancelHold();
        }
    }

    private void OnUnlockHoldCompleted()
    {
        Unlock(cachedInventory);
        ToggleDoorState(cachedInventory);
    }

    private void ToggleDoorState(IKeyInventory playerInventory)
    {
        toggle = !toggle;
        UpdatePromptMessage(playerInventory);

        OpenDoor();

        float targetAngle = toggle ? DoorOpenAngle : DoorCloseAngle;

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(AnimateRotation(targetAngle));
    }

    private void Unlock(IKeyInventory playerInventory)
    {
        if (consumeKeyOnUse && playerInventory != null)
        {
            playerInventory.ConsumeKey(requiredKeyID);
        }

        isLocked = false;

        if (asource != null && unlockDoorSound != null)
        {
            asource.PlayOneShot(unlockDoorSound);
        }
    }

    private void OnAttemptOpenLocked()
    {
        if (asource != null && lockedDoorSound != null)
        {
            asource.PlayOneShot(lockedDoorSound);
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeDoorHandle());
    }

    private IEnumerator ShakeDoorHandle()
    {
        Quaternion originalRotation = doorMeshTransform.localRotation;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float zOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            doorMeshTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, zOffset);
            elapsed += Time.deltaTime;
            yield return null;
        }

        doorMeshTransform.localRotation = originalRotation;
    }

    public void ForceOpen()
    {
        if (open) return;

        if (holdInteraction != null)
        {
            holdInteraction.ResetHold();
        }

        isLocked = false;
        toggle = true;
        open = true;
        UpdatePromptMessage(null);

        UpdateNavObstacleState();

        if (asource != null && openDoor != null)
        {
            asource.clip = openDoor;
            asource.Play();
        }

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(AnimateRotation(DoorOpenAngle));
    }

    private IEnumerator AnimateRotation(float targetAngle)
    {
        Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);

        while (Quaternion.Angle(doorMeshTransform.localRotation, targetRotation) > 0.1f)
        {
            doorMeshTransform.localRotation = Quaternion.Slerp(
                doorMeshTransform.localRotation,
                targetRotation,
                Time.deltaTime * 5f * smooth
            );
            yield return null;
        }

        doorMeshTransform.localRotation = targetRotation;
    }

    public void OpenDoor()
    {
        open = !open;
        UpdateNavObstacleState();

        if (asource != null)
        {
            asource.clip = open ? openDoor : closeDoor;
            asource.Play();
        }
    }

    private void UpdateNavObstacleState()
    {
        if (navObstacle == null) return;

        if (open)
        {
            navObstacle.carving = false;
            navObstacle.enabled = false;
        }
        else
        {
            navObstacle.enabled = true;
            navObstacle.carving = true;
        }
    }
}