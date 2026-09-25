using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class DoorScript : MonoBehaviour, IInteractable
{
    [Header("Door Movement")]
    public bool open;
    public float smooth = 0.5f;
    public float DoorOpenAngle = 90.0f;
    public float DoorCloseAngle = 0.0f;

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string requiredKeyID = "";
    [SerializeField] private bool consumeKeyOnUse = true;

    [Header("Audio")]
    public AudioSource asource;
    public AudioClip openDoor, closeDoor, lockedDoorSound, unlockDoorSound;

    [Header("Juice / Visual Feedback")]
    [SerializeField] private Transform doorMeshTransform;
    [SerializeField] private Transform handleTransform;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 2.0f;
    [SerializeField] private Vector3 handleRotateOffset = new Vector3(0f, 0f, -45f);
    [SerializeField] private float handleAnimationTime = 0.15f;

    public string promptMessage { get; private set; }

    public bool toggle;
    private Coroutine rotationCoroutine;
    private Coroutine shakeCoroutine;
    private Coroutine handleCoroutine;

    private HoldInteraction holdInteraction;
    private IKeyInventory currentInventoryHolding;

    public bool IsLocked => isLocked;
    public bool IsUnlocking => holdInteraction != null && holdInteraction.IsHolding;

    private void Start()
    {
        asource = GetComponent<AudioSource>();

        if (doorMeshTransform == null)
        {
            doorMeshTransform = transform;
        }

        holdInteraction = GetComponent<HoldInteraction>();
        if (holdInteraction != null)
        {
            holdInteraction.onHoldCompleted.AddListener(OnHoldUnlockComplete);
        }

        UpdatePromptMessage(null);
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
        if (isLocked)
        {
            if (playerInventory != null && playerInventory.HasKey(requiredKeyID))
            {
                if (holdInteraction != null)
                {
                    currentInventoryHolding = playerInventory;
                    holdInteraction.BeginHold();
                }
                else
                {
                    Unlock(playerInventory);
                }
            }
            else
            {
                OnAttemptOpenLocked();
            }
            return;
        }

        TriggerHandleAnimation();

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

    public void CancelInteract()
    {
        if (holdInteraction != null && holdInteraction.IsHolding)
        {
            holdInteraction.CancelHold();
            currentInventoryHolding = null;
        }
    }

    private void OnHoldUnlockComplete()
    {
        Unlock(currentInventoryHolding);
    }

    private void Unlock(IKeyInventory playerInventory)
    {
        if (consumeKeyOnUse && playerInventory != null)
        {
            playerInventory.ConsumeKey(requiredKeyID);
        }

        isLocked = false;
        currentInventoryHolding = null;

        UpdatePromptMessage(null);

        TriggerHandleAnimation();

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
        if (handleTransform == null) yield break;

        Quaternion originalRotation = handleTransform.localRotation;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float zOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            handleTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, zOffset);
            elapsed += Time.deltaTime;
            yield return null;
        }

        handleTransform.localRotation = originalRotation;
    }

    private void TriggerHandleAnimation()
    {
        if (handleTransform == null) return;

        if (handleCoroutine != null)
        {
            StopCoroutine(handleCoroutine);
        }
        handleCoroutine = StartCoroutine(AnimateHandleDown());
    }

    private IEnumerator AnimateHandleDown()
    {
        Quaternion originalRotation = handleTransform.localRotation;
        Quaternion targetRotation = originalRotation * Quaternion.Euler(handleRotateOffset);
        float elapsed = 0.0f;

        while (elapsed < handleAnimationTime)
        {
            float t = elapsed / handleAnimationTime;
            handleTransform.localRotation = Quaternion.Slerp(originalRotation, targetRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        handleTransform.localRotation = targetRotation;

        elapsed = 0.0f;
        while (elapsed < handleAnimationTime)
        {
            float t = elapsed / handleAnimationTime;
            handleTransform.localRotation = Quaternion.Slerp(targetRotation, originalRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        handleTransform.localRotation = originalRotation;
    }

    public void ForceOpen()
    {
        if (open) return;

        isLocked = false;
        toggle = true;
        open = true;
        UpdatePromptMessage(null);

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

        if (asource != null)
        {
            asource.clip = open ? openDoor : closeDoor;
            asource.Play();
        }
    }
}