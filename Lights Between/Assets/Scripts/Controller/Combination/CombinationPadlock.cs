using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class CombinationPadlock : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public struct PadlockDial
    {
        public Transform dialTransform;
        [HideInInspector] public int currentValue;
    }

    [Header("Inspect Setup")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private GameObject inspectPadlockPrefab;
    [SerializeField] private GameObject puzzleUIPanel;

    [Header("Power & Inspection Light")]
    [SerializeField] private ElectricBoxBreaker breakerBox;
    [SerializeField] private GameObject inspectionLightObject;

    [Header("Accessibility & Text UI")]
    [SerializeField] private TMP_Text combinationDisplayTMP;
    [SerializeField] private InputTextPrompt horizontalPromptUI;
    [SerializeField] private InputTextPrompt verticalPromptUI;
    [SerializeField] private InputTextPrompt confirmPromptUI;

    [Header("UI Selection Indicator")]
    [SerializeField] private RectTransform selectionArrowUI;
    [SerializeField] private float[] dialXPositions = new float[5] { -200f, -100f, 0f, 100f, 200f };

    [Header("Inspection Offsets")]
    [SerializeField] private Vector3 inspectionPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 inspectionRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 inspectionScale = Vector3.one;

    [Header("Lock Configuration")]
    [SerializeField] private PadlockDial[] dials = new PadlockDial[5];
    [SerializeField] private int[] correctCode = new int[5] { 1, 9, 2, 5, 0 };

    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = new Vector3(1f, 0f, 0f);
    [SerializeField] private float rotationSpeed = 12.0f;
    [SerializeField] private float degreesPerStep = 36.0f;

    [Header("Overall Model Outline Feedback")]
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
    private FPSController fpsController;
    private GameObject currentInspectInstance;
    private Outline overallModelOutline;
    private int selectedDialIndex = 0;
    private bool isInspecting = false;
    private bool isUnlocked = false;
    private bool isBusy = false;

    private bool navInputAxisInUse = false;
    private bool rotateInputAxisInUse = false;

    private void Awake()
    {
        inputHandler = FindFirstObjectByType<InputHandler>();
        fpsController = FindFirstObjectByType<FPSController>();

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }

        if (selectionArrowUI != null)
        {
            selectionArrowUI.gameObject.SetActive(false);
        }

        if (inspectionLightObject != null)
        {
            inspectionLightObject.SetActive(false);
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

        if (fpsController != null)
        {
            fpsController.SetMiniGameMode(true);
            fpsController.ForceLookAt(transform.position + transform.forward * 2.0f);
        }

        if (inspectPadlockPrefab != null && holdPoint != null)
        {
            currentInspectInstance = Instantiate(inspectPadlockPrefab, holdPoint);
            currentInspectInstance.transform.localPosition = inspectionPositionOffset;
            currentInspectInstance.transform.localRotation = Quaternion.Euler(inspectionRotationOffset);
            currentInspectInstance.transform.localScale = inspectionScale;

            overallModelOutline = currentInspectInstance.GetComponent<Outline>();
            if (overallModelOutline == null)
            {
                overallModelOutline = currentInspectInstance.GetComponentInChildren<Outline>();
            }

            if (overallModelOutline != null)
            {
                overallModelOutline.enabled = true;
                overallModelOutline.OutlineColor = activeOutlineColor;
            }

            CacheSpawnedDials();
        }

        EvaluateInspectionLight();

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
        }

        if (selectionArrowUI != null)
        {
            selectionArrowUI.gameObject.SetActive(true);
        }

        selectedDialIndex = 0;
        UpdateArrowUIPosition();
        UpdateCombinationTextDisplay();

        Debug.Log($"[Padlock] Entered Inspect Mode. Selected Dial Index: {selectedDialIndex} | Current Value: {dials[selectedDialIndex].currentValue}");
    }

    public void ExitInspectMode()
    {
        if (!isInspecting) return;

        isInspecting = false;

        if (inspectionLightObject != null)
        {
            inspectionLightObject.SetActive(false);
        }

        if (selectionArrowUI != null)
        {
            selectionArrowUI.gameObject.SetActive(false);
        }

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }

        if (currentInspectInstance != null)
        {
            Destroy(currentInspectInstance);
        }

        if (fpsController != null)
        {
            fpsController.SetMiniGameMode(false);
        }

        if (inputHandler != null)
        {
            inputHandler.ResetInputValues();
        }

        Debug.Log("[Padlock] Exited Inspect Mode.");
    }

    private void EvaluateInspectionLight()
    {
        if (inspectionLightObject == null) return;

        bool isPowerOn = breakerBox != null && breakerBox.IsGridActive;
        inspectionLightObject.SetActive(!isPowerOn);
    }

    private void CacheSpawnedDials()
    {
        if (currentInspectInstance == null) return;

        PadlockInspectContainer container = currentInspectInstance.GetComponent<PadlockInspectContainer>();
        if (container != null)
        {
            if (container.GetDials() != null && container.GetDials().Length == dials.Length)
            {
                dials = container.GetDials();
            }

            if (container.GetDialXPositions() != null && container.GetDialXPositions().Length == dialXPositions.Length)
            {
                dialXPositions = container.GetDialXPositions();
            }
        }
    }

    private void UpdateArrowUIPosition()
    {
        if (selectionArrowUI == null || dialXPositions == null || dialXPositions.Length == 0 || selectedDialIndex >= dialXPositions.Length) return;

        Vector2 anchoredPos = selectionArrowUI.anchoredPosition;
        anchoredPos.x = dialXPositions[selectedDialIndex];
        selectionArrowUI.anchoredPosition = anchoredPos;
    }

    private void UpdateCombinationTextDisplay()
    {
        if (combinationDisplayTMP == null || dials == null || dials.Length == 0) return;

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < dials.Length; i++)
        {
            sb.Append(dials[i].currentValue);
            if (i < dials.Length - 1) sb.Append(" ");
        }

        combinationDisplayTMP.text = sb.ToString();
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

        Vector2 navVector = inputHandler.navigateDial.sqrMagnitude > 0.1f ? inputHandler.navigateDial : inputHandler.move;

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

        if (inputHandler.confirmDial || inputHandler.interact)
        {
            inputHandler.confirmDial = false;
            inputHandler.interact = false;
            EvaluateCombination();
        }
    }

    private void ChangeSelectedDial(int direction)
    {
        selectedDialIndex += direction;

        if (selectedDialIndex >= dials.Length) selectedDialIndex = 0;
        if (selectedDialIndex < 0) selectedDialIndex = dials.Length - 1;

        UpdateArrowUIPosition();

        Debug.Log($"[Padlock] Dial Changed -> Index: {selectedDialIndex} | Target Pos X: {dialXPositions[selectedDialIndex]} | Current Value: {dials[selectedDialIndex].currentValue}");
    }

    private IEnumerator RotateDialRoutine(int dialIndex, int direction)
    {
        isBusy = true;

        dials[dialIndex].currentValue = (dials[dialIndex].currentValue + direction) % 10;
        if (dials[dialIndex].currentValue < 0) dials[dialIndex].currentValue += 10;

        UpdateCombinationTextDisplay();

        Debug.Log($"[Padlock] Rotated Dial [{dialIndex}] -> New Value: {dials[dialIndex].currentValue}");

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
        if (dials == null || correctCode == null || dials.Length != correctCode.Length)
        {
            Debug.LogWarning("[Padlock] Dials count does not match Correct Code count!");
            return;
        }

        StringBuilder currentAttemptSB = new StringBuilder();
        StringBuilder expectedCodeSB = new StringBuilder();
        bool isCorrect = true;

        for (int i = 0; i < dials.Length; i++)
        {
            currentAttemptSB.Append(dials[i].currentValue);
            expectedCodeSB.Append(correctCode[i]);

            if (i < dials.Length - 1)
            {
                currentAttemptSB.Append("-");
                expectedCodeSB.Append("-");
            }

            if (dials[i].currentValue != correctCode[i])
            {
                isCorrect = false;
            }
        }

        Debug.Log($"[Padlock] Confirm Pressed. Current Input: [{currentAttemptSB}] | Expected Code: [{expectedCodeSB}]");

        StartCoroutine(EvaluateRoutine(isCorrect));
    }

    private IEnumerator EvaluateRoutine(bool isCorrect)
    {
        isBusy = true;

        if (overallModelOutline != null)
        {
            overallModelOutline.OutlineColor = isCorrect ? correctOutlineColor : wrongOutlineColor;
        }

        if (isCorrect)
        {
            Debug.Log("<color=green>[Padlock] Code Correct! Unlocking...</color>");

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
            Debug.Log("<color=red>[Padlock] Code Incorrect!</color>");

            if (audioSource != null && lockErrorSFX != null)
            {
                audioSource.PlayOneShot(lockErrorSFX);
            }

            yield return new WaitForSeconds(1.2f);

            if (overallModelOutline != null)
            {
                overallModelOutline.OutlineColor = activeOutlineColor;
            }

            isBusy = false;
        }
    }
}