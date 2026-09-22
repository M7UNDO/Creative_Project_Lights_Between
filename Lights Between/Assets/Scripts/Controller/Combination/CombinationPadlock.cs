using System.Collections;
using UnityEngine;

public class CombinationPadlock : MonoBehaviour
{
    [System.Serializable]
    public struct PadlockDial
    {
        public Transform dialTransform;
        public Outline dialOutline;
        [HideInInspector] public int currentValue;
    }

    [Header("Inspect Setup")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private GameObject inspectPadlockPrefab;
    [SerializeField] private GameObject puzzleUIPanel;

    [Header("Lock Configuration")]
    [SerializeField] private PadlockDial[] dials = new PadlockDial[4];
    [SerializeField] private int[] correctCode = new int[4] { 1, 9, 2, 5 };

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = new Vector3(1f, 0f, 0f);
    [SerializeField] private float rotationSpeed = 12.0f;
    [SerializeField] private float degreesPerStep = 36.0f;

    [Header("Outline Colors")]
    [SerializeField] private Color activeOutlineColor = Color.yellow;
    [SerializeField] private Color correctOutlineColor = Color.green;
    [SerializeField] private Color wrongOutlineColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialTurnSFX;
    [SerializeField] private AudioClip lockSuccessSFX;
    [SerializeField] private AudioClip lockErrorSFX;

    [Header("Events")]
    [SerializeField] private GameEventSO onLockUnlockedEvent;

    private InputHandler inputHandler;
    private GameObject currentInspectInstance;
    private int selectedDialIndex = 0;
    private bool isInspecting = false;
    private bool isUnlocked = false;
    private bool isBusy = false;

    private bool navInputAxisInUse = false;
    private bool rotateInputAxisInUse = false;

    private void Awake()
    {
        inputHandler = FindFirstObjectByType<InputHandler>();

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isInspecting || isUnlocked || isBusy) return;

        HandleInspectInputs();
    }

    public void Interact()
    {
        if (isUnlocked || isInspecting) return;

        EnterInspectMode();
    }

    private void EnterInspectMode()
    {
        isInspecting = true;

        if (inputHandler != null)
        {
            inputHandler.blockLookInput = true;
        }

        if (inspectPadlockPrefab != null && holdPoint != null)
        {
            currentInspectInstance = Instantiate(inspectPadlockPrefab, holdPoint.position, holdPoint.rotation, holdPoint);
            CacheInspectInstanceDials();
        }

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
        }

        selectedDialIndex = 0;
        UpdateDialOutlines();
    }

    public void ExitInspectMode()
    {
        if (!isInspecting) return;

        isInspecting = false;

        if (currentInspectInstance != null)
        {
            Destroy(currentInspectInstance);
        }

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }

        if (inputHandler != null)
        {
            inputHandler.blockLookInput = false;
            inputHandler.ResetInputValues();
        }
    }

    private void CacheInspectInstanceDials()
    {
        if (currentInspectInstance == null) return;

        PadlockDial[] inspectDials = currentInspectInstance.GetComponentsInChildren<PadlockDialHolder>() != null
            ? currentInspectInstance.GetComponent<PadlockInspectContainer>()?.GetDials()
            : null;

        if (inspectDials != null && inspectDials.Length == dials.Length)
        {
            dials = inspectDials;
        }
    }

    private void HandleInspectInputs()
    {
        if (inputHandler == null) return;

        if (inputHandler.cancel || inputHandler.close)
        {
            inputHandler.cancel = false;
            inputHandler.close = false;
            ExitInspectMode();
            return;
        }

        Vector2 navVector = inputHandler.wheelNavigate.sqrMagnitude > 0.1f ? inputHandler.wheelNavigate : inputHandler.move;

        if (Mathf.Abs(navVector.x) > 0.5f)
        {
            if (!navInputAxisInUse)
            {
                navInputAxisInUse = true;
                int direction = navVector.x > 0 ? 1 : -1;
                ChangeSelectedDial(direction);
            }
        }
        else
        {
            navInputAxisInUse = false;
        }

        if (Mathf.Abs(navVector.y) > 0.5f)
        {
            if (!rotateInputAxisInUse)
            {
                rotateInputAxisInUse = true;
                int direction = navVector.y > 0 ? 1 : -1;
                StartCoroutine(RotateDialRoutine(selectedDialIndex, direction));
            }
        }
        else
        {
            rotateInputAxisInUse = false;
        }

        if (inputHandler.interact || inputHandler.timedAction)
        {
            inputHandler.interact = false;
            inputHandler.timedAction = false;
            EvaluateCombination();
        }
    }

    private void ChangeSelectedDial(int direction)
    {
        selectedDialIndex += direction;

        if (selectedDialIndex >= dials.Length) selectedDialIndex = 0;
        if (selectedDialIndex < 0) selectedDialIndex = dials.Length - 1;

        UpdateDialOutlines();
    }

    private void UpdateDialOutlines()
    {
        for (int i = 0; i < dials.Length; i++)
        {
            if (dials[i].dialOutline == null) continue;

            if (i == selectedDialIndex)
            {
                dials[i].dialOutline.enabled = true;
                dials[i].dialOutline.OutlineColor = activeOutlineColor;
            }
            else
            {
                dials[i].dialOutline.enabled = false;
            }
        }
    }

    private IEnumerator RotateDialRoutine(int dialIndex, int direction)
    {
        isBusy = true;

        dials[dialIndex].currentValue = (dials[dialIndex].currentValue + direction) % 10;
        if (dials[dialIndex].currentValue < 0) dials[dialIndex].currentValue += 10;

        if (audioSource != null && dialTurnSFX != null)
        {
            audioSource.PlayOneShot(dialTurnSFX);
        }

        Quaternion startRotation = dials[dialIndex].dialTransform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(rotationAxis * (degreesPerStep * direction));

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * rotationSpeed;
            dials[dialIndex].dialTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed);
            yield return null;
        }

        dials[dialIndex].dialTransform.localRotation = targetRotation;
        isBusy = false;
    }

    private void EvaluateCombination()
    {
        bool isCorrect = true;

        for (int i = 0; i < dials.Length; i++)
        {
            if (dials[i].currentValue != correctCode[i])
            {
                isCorrect = false;
                break;
            }
        }

        StartCoroutine(EvaluateRoutine(isCorrect));
    }

    private IEnumerator EvaluateRoutine(bool isCorrect)
    {
        isBusy = true;

        Color targetColor = isCorrect ? correctOutlineColor : wrongOutlineColor;

        for (int i = 0; i < dials.Length; i++)
        {
            if (dials[i].dialOutline != null)
            {
                dials[i].dialOutline.enabled = true;
                dials[i].dialOutline.OutlineColor = targetColor;
            }
        }

        if (isCorrect)
        {
            if (audioSource != null && lockSuccessSFX != null)
            {
                audioSource.PlayOneShot(lockSuccessSFX);
            }

            yield return new WaitForSeconds(1.0f);

            isUnlocked = true;

            if (onLockUnlockedEvent != null)
            {
                onLockUnlockedEvent.Raise();
            }

            ExitInspectMode();
            gameObject.SetActive(false);
        }
        else
        {
            if (audioSource != null && lockErrorSFX != null)
            {
                audioSource.PlayOneShot(lockErrorSFX);
            }

            yield return new WaitForSeconds(1.2f);

            isBusy = false;
            UpdateDialOutlines();
        }
    }
}