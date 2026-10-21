using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BreakerLever : MonoBehaviour, IInteractable
{
    [SerializeField] private ElectricBoxBreaker connectedBreaker;

    [SerializeField] private bool isLeverUp = true;
    [SerializeField] private float smoothSpeed = 1.5f;

    [SerializeField] private Transform leverHandleTransform;
    [SerializeField] private Renderer[] leverHandleRenderers;
    [SerializeField] private Collider leverCollider;
    [SerializeField] private GameObject leverWorldSpaceUI;

    [SerializeField] private Vector3 leverOffRotationEuler;
    [SerializeField] private Vector3 leverOnRotationEuler;

    [Header("Missing Lever Settings")]
    [SerializeField] private string missingPromptMessage = "Inspect Breaker";
    [SerializeField] private DialogueSO missingLeverDialogueSO;
    [TextArea(2, 4)]
    [SerializeField]
    private string[] missingLeverDialogue = new string[]
    {
        "The lever's been torn right off.",
        "I need to find something that fits into this slot to turn the power back on."
    };

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchOnSFX;
    [SerializeField] private AudioClip switchOffSFX;

    public event Action OnLeverStateChanged;

    public string promptMessage { get; private set; }
    public bool IsLeverUp => isLeverUp;

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotationCoroutine;
    private bool hasPlayedMissingDialogue = false;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        offRotation = Quaternion.Euler(leverOffRotationEuler);
        onRotation = Quaternion.Euler(leverOnRotationEuler);

        if (leverHandleTransform != null)
        {
            leverHandleTransform.localRotation = isLeverUp ? onRotation : offRotation;
        }

        if (leverCollider == null && leverHandleTransform != null)
        {
            leverCollider = leverHandleTransform.GetComponent<Collider>();
        }

        if (leverHandleRenderers == null || leverHandleRenderers.Length == 0)
        {
            if (leverHandleTransform != null)
            {
                leverHandleRenderers = leverHandleTransform.GetComponentsInChildren<Renderer>(true);
            }
        }

        UpdateVisualsAndPrompts();
    }

    private void OnEnable()
    {
        RegisterDialogueEvent();
    }

    private void OnDisable()
    {
        UnregisterDialogueEvent();
    }

    private void RegisterDialogueEvent()
    {
        if (missingLeverDialogueSO != null && missingLeverDialogueSO.OnDialogueCompletedEvent != null)
        {
            missingLeverDialogueSO.OnDialogueCompletedEvent.RegisterListener(OnDialogueCompleted);
        }
    }

    private void UnregisterDialogueEvent()
    {
        if (missingLeverDialogueSO != null && missingLeverDialogueSO.OnDialogueCompletedEvent != null)
        {
            missingLeverDialogueSO.OnDialogueCompletedEvent.UnregisterListener(OnDialogueCompleted);
        }
    }

    private void OnDialogueCompleted()
    {
        if (connectedBreaker != null)
        {
            connectedBreaker.UnlockRepairSnapZone();
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ActivateEnemy(EnemyAI.PatrolZone.Basement);
        }
    }

    public bool IsLeverUnlocked()
    {
        bool isMissing = connectedBreaker != null && connectedBreaker.IsLeverMissing;

        if (!isMissing)
        {
            return true;
        }

        return !hasPlayedMissingDialogue;
    }

    public void Interact()
    {
        bool isMissing = connectedBreaker != null && connectedBreaker.IsLeverMissing;

        if (isMissing)
        {
            if (!hasPlayedMissingDialogue)
            {
                hasPlayedMissingDialogue = true;
                TriggerMissingDialogue();
                UpdateVisualsAndPrompts();
            }
            return;
        }

        isLeverUp = !isLeverUp;
        UpdateVisualsAndPrompts();
        PlaySwitchSFX(isLeverUp);
        StartLeverAnimation();

        NotifyStateChanged();
    }

    private void TriggerMissingDialogue()
    {
        if (DialogueManager.Instance == null) return;

        if (missingLeverDialogueSO != null)
        {
            DialogueManager.Instance.StartDialogue(missingLeverDialogueSO);
        }
        else if (missingLeverDialogue != null && missingLeverDialogue.Length > 0)
        {
            DialogueManager.Instance.StartCustomLines(missingLeverDialogue, OnDialogueCompleted, managePlayerMode: true);
        }
    }

    public void SetLeverUpState(bool leverUp, bool animate = true)
    {
        isLeverUp = leverUp;
        UpdateVisualsAndPrompts();

        if (animate)
        {
            StartLeverAnimation();
        }
        else if (leverHandleTransform != null)
        {
            leverHandleTransform.localRotation = isLeverUp ? onRotation : offRotation;
        }

        NotifyStateChanged();
    }

    public void TripLever()
    {
        if (!isLeverUp || (connectedBreaker != null && connectedBreaker.IsLeverMissing)) return;

        isLeverUp = false;
        UpdateVisualsAndPrompts();
        PlaySwitchSFX(false);
        StartLeverAnimation();

        NotifyStateChanged();
    }

    public void UpdateVisualsAndPrompts()
    {
        bool isMissing = connectedBreaker != null && connectedBreaker.IsLeverMissing;
        bool showLeverVisuals = !isMissing;

        if (leverHandleRenderers != null)
        {
            for (int i = 0; i < leverHandleRenderers.Length; i++)
            {
                if (leverHandleRenderers[i] != null)
                {
                    leverHandleRenderers[i].enabled = showLeverVisuals;
                }
            }
        }

        if (leverWorldSpaceUI != null)
        {
            leverWorldSpaceUI.SetActive(showLeverVisuals && IsLeverUnlocked());
        }

        if (leverCollider != null)
        {
            leverCollider.enabled = true;
        }

        if (isMissing)
        {
            promptMessage = !hasPlayedMissingDialogue ? missingPromptMessage : string.Empty;
        }
        else
        {
            promptMessage = isLeverUp ? "Pull down" : "Pull up";
        }
    }

    private void PlaySwitchSFX(bool turningOn)
    {
        if (audioSource == null) return;

        AudioClip primarySFX = turningOn ? switchOnSFX : switchOffSFX;
        if (primarySFX != null)
        {
            audioSource.PlayOneShot(primarySFX);
        }
    }

    private void StartLeverAnimation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(AnimateLever());
    }

    private IEnumerator AnimateLever()
    {
        Quaternion targetRotation = isLeverUp ? onRotation : offRotation;

        while (leverHandleTransform != null && Quaternion.Angle(leverHandleTransform.localRotation, targetRotation) > 0.1f)
        {
            leverHandleTransform.localRotation = Quaternion.Slerp(
                leverHandleTransform.localRotation,
                targetRotation,
                Time.deltaTime * 8f * smoothSpeed
            );
            yield return null;
        }

        if (leverHandleTransform != null)
        {
            leverHandleTransform.localRotation = targetRotation;
        }
    }

    private void NotifyStateChanged()
    {
        OnLeverStateChanged?.Invoke();
        if (connectedBreaker != null)
        {
            connectedBreaker.RefreshPowerGrid();
        }
    }
}