using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputHandler))]
[RequireComponent(typeof(FPSController))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Canvas hudCanvas;
    private CanvasGroup hudCanvasGroup;
    [SerializeField] private GameObject dropUI;
    [SerializeField] public float interactionRange = 5f;
    [SerializeField] private LayerMask interactionLayer;
    [SerializeField] private LayerMask occlusionLayer;

    [SerializeField] private Image defaultCrosshair;
    [SerializeField] private Image interactableCrosshair;

    public bool isInteractable = false;
    private IInteractable currentInteractable;
    private IInteractable _lastInteractable;
    private IKeyInventory _keyInventory;
    private WorldSpaceInteractableUI _currentWorldUI;

    [Header("Grab Settings")]
    [SerializeField] private LayerMask heldLayer;
    [SerializeField] private Transform objectGrabPointTransform;

    private ObjectGrabbable _currentGrabbable;
    public ObjectGrabbable CurrentGrabbable => _currentGrabbable;

    private InputHandler _input;
    private FPSController _fpsController;
    private GameObject _mainCamera;

    private void Awake()
    {
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        _input = GetComponent<InputHandler>();
        _fpsController = GetComponent<FPSController>();
        _keyInventory = GetComponent<IKeyInventory>();

        if (_keyInventory == null)
        {
            _keyInventory = GetComponentInChildren<IKeyInventory>();
        }
    }

    private void Update()
    {
        if (_fpsController.isInMiniGame || _fpsController.isInspecting || _fpsController.isJumpscareActive || _fpsController.isDialogueActive || PauseManager.IsGamePaused || NoteUIController.isNoteOpen)
        {
            // Forces World Space Prompts and Interaction Crosshairs to hide while busy
            HideActiveWorldUI();
            return;
        }

        DetectInteractable();
        UpdateCrosshair();
        Interact();
    }

    private void HideActiveWorldUI()
    {
        if (_currentWorldUI != null)
        {
            _currentWorldUI.HidePrompt();
            _currentWorldUI = null;
        }

        if (isInteractable)
        {
            isInteractable = false;
            UpdateCrosshair();
        }
    }

    private void DetectInteractable()
    {
        WorldSpaceInteractableUI targetedWorldUI = null;

        currentInteractable = null;
        isInteractable = false;

        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer, QueryTriggerInteraction.Collide))
        {
            ObjectGrabbable grabbable = hit.collider.GetComponentInParent<ObjectGrabbable>();

            if (_currentGrabbable != null && grabbable == _currentGrabbable)
            {
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            bool hasInteractable = interactable != null && (!(interactable is MonoBehaviour mb) || mb.enabled);
            bool hasPower = hit.collider.TryGetComponent<IToolPower>(out var powerItem);
            bool hasGrabbable = grabbable != null && grabbable.enabled && _currentGrabbable == null;

            if (hasInteractable || hasPower || hasGrabbable)
            {
                if (interactable is PlayerPoseAnchor poseAnchor)
                {
                    if (_currentGrabbable != null || !poseAnchor.IsAnchorUnlocked()) return;
                }

                if (interactable is ToolPickup toolPickup)
                {
                    if (!toolPickup.IsPickupUnlocked()) return;
                }

                if (hasGrabbable && _fpsController.isSeated) return;

                if (hasInteractable)
                {
                    currentInteractable = interactable;
                    isInteractable = true;
                }
                else if (hasGrabbable)
                {
                    isInteractable = true;
                }

                string basePrompt = "";
                bool canCurrentlyInteract = true;

                if (hit.collider.TryGetComponent<SnapZone>(out SnapZone snapZone))
                {
                    basePrompt = snapZone.GetPromptMessage(_currentGrabbable);
                    canCurrentlyInteract = snapZone.CanSnap(_currentGrabbable);
                    isInteractable = canCurrentlyInteract;
                }
                else if (hit.collider.TryGetComponent<DoorScript>(out DoorScript doorScript))
                {
                    doorScript.UpdatePromptMessage(_keyInventory);
                    basePrompt = doorScript.promptMessage;
                    canCurrentlyInteract = doorScript.CanInteract(_keyInventory);
                }
                else if (hit.collider.TryGetComponent<GeneratorPowerSystem>(out GeneratorPowerSystem generator))
                {
                    basePrompt = generator.promptMessage;
                    canCurrentlyInteract = generator.CanInteract();
                }
                else if (hit.collider.TryGetComponent<ToolPickup>(out ToolPickup pickup))
                {
                    basePrompt = pickup.PromptMessage;
                    canCurrentlyInteract = true;
                }
                else if (hit.collider.TryGetComponent<ElectricBoxBreaker>(out ElectricBoxBreaker electricBox))
                {
                    basePrompt = electricBox.promptMessage;
                    canCurrentlyInteract = true;
                }
                else if (hit.collider.TryGetComponent<BaseOpenable>(out BaseOpenable openable))
                {
                    basePrompt = openable.promptMessage;
                    canCurrentlyInteract = true;
                }
                else if (hasGrabbable)
                {
                    basePrompt = "Grab";
                    canCurrentlyInteract = true;
                }
                else if (hasInteractable)
                {
                    basePrompt = "Interact";
                    canCurrentlyInteract = true;
                }

                if (hasPower && powerItem.UsesPower)
                {
                    int powerPercent = Mathf.FloorToInt((powerItem.CurrentPower / powerItem.MaxPower) * 100f);

                    if (!string.IsNullOrEmpty(basePrompt))
                    {
                        basePrompt = $"{basePrompt} {powerPercent}%";
                    }
                    else
                    {
                        basePrompt = $"Paraffin Lamp {powerPercent}%";
                    }
                }

                Transform interactionRoot = null;
                if (hasInteractable && interactable is MonoBehaviour rootMb)
                {
                    interactionRoot = rootMb.transform;
                }
                else if (hasGrabbable)
                {
                    interactionRoot = grabbable.transform;
                }
                else
                {
                    interactionRoot = hit.collider.transform;
                }

                WorldSpaceInteractableUI[] nestedUIs = interactionRoot.GetComponentsInChildren<WorldSpaceInteractableUI>(true);
                foreach (WorldSpaceInteractableUI ui in nestedUIs)
                {
                    IInteractable parentInteractable = ui.GetComponentInParent<IInteractable>();
                    ObjectGrabbable parentGrabbable = ui.GetComponentInParent<ObjectGrabbable>();

                    Transform closestOwner = null;
                    if (parentInteractable != null && parentInteractable is MonoBehaviour pMb)
                    {
                        closestOwner = pMb.transform;
                    }
                    else if (parentGrabbable != null)
                    {
                        closestOwner = parentGrabbable.transform;
                    }

                    if (closestOwner == interactionRoot)
                    {
                        targetedWorldUI = ui;
                        break;
                    }
                }

                if (targetedWorldUI != null && !_fpsController.isInspecting)
                {
                    targetedWorldUI.ShowPrompt(basePrompt, isInteractable, canCurrentlyInteract);
                }
            }
        }

        if (currentInteractable != _lastInteractable)
        {
            if (_lastInteractable is DoorScript lastDoor)
            {
                lastDoor.CancelInteract();
            }
        }

        _lastInteractable = currentInteractable;

        Vector3 origin = _mainCamera.transform.position;

        foreach (var ui in WorldSpaceInteractableUI.AllInteractableUIs)
        {
            if (ui == null || !ui.enabled || !ui.gameObject.activeInHierarchy) continue;
            if (ui == targetedWorldUI) continue;

            ToolPickup parentPickup = ui.GetComponentInParent<ToolPickup>();
            if (parentPickup != null && !parentPickup.IsPickupUnlocked())
            {
                ui.HidePrompt();
                continue;
            }

            PlayerPoseAnchor parentAnchor = ui.GetComponentInParent<PlayerPoseAnchor>();
            if (parentAnchor != null && !parentAnchor.IsAnchorUnlocked())
            {
                ui.HidePrompt();
                continue;
            }

            float distance = Vector3.Distance(transform.position, ui.transform.position);

            if (distance <= ui.ProximityDistance)
            {
                Vector3 targetPos = ui.transform.position;
                Vector3 direction = (targetPos - origin).normalized;
                float checkDistance = Vector3.Distance(origin, targetPos);

                if (Physics.Raycast(origin, direction, out RaycastHit losHit, checkDistance, occlusionLayer, QueryTriggerInteraction.Ignore))
                {
                    ui.HidePrompt();
                }
                else
                {
                    ui.ShowSkeleton(true);
                }
            }
            else
            {
                ui.HidePrompt();
            }
        }

        _currentWorldUI = targetedWorldUI;
    }

    private void Interact()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        if (_input.drop)
        {
            if (_currentGrabbable != null)
            {
                _currentGrabbable.Drop();
                _currentGrabbable = null;

                if (dropUI != null) dropUI.SetActive(false);
            }
            _input.drop = false;
        }

        if (_input.interact)
        {
            if (currentInteractable != null && (!(currentInteractable is MonoBehaviour mb) || mb.enabled))
            {
                if (currentInteractable is SnapZone snapZone)
                {
                    snapZone.Interact();
                    _input.interact = false;
                }
                else if (currentInteractable is DoorScript door)
                {
                    door.Interact(_keyInventory);

                    if (!door.IsUnlocking)
                    {
                        _input.interact = false;
                    }
                }
                else
                {
                    currentInteractable.Interact();

                    if (currentInteractable is not GeneratorPowerSystem)
                    {
                        _input.interact = false;
                    }
                }
            }
            else if (_currentGrabbable == null && !_fpsController.isSeated)
            {
                Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer, QueryTriggerInteraction.Collide))
                {
                    ObjectGrabbable grabbable = hit.collider.GetComponentInParent<ObjectGrabbable>();
                    if (grabbable != null && grabbable.enabled)
                    {
                        _currentGrabbable = grabbable;
                        _currentGrabbable.Grab(objectGrabPointTransform, GetLayerFromMask(heldLayer));
                        _input.interact = false;

                        if (dropUI != null) dropUI.SetActive(true);
                    }
                }
            }
        }
        else
        {
            if (currentInteractable is DoorScript door)
            {
                door.CancelInteract();
            }
        }
    }

    public void ForceEquipGrabbable(ObjectGrabbable grabbable)
    {
        if (grabbable == null) return;

        _currentGrabbable = grabbable;
        _currentGrabbable.Grab(objectGrabPointTransform, GetLayerFromMask(heldLayer));

        if (dropUI != null) dropUI.SetActive(true);
    }

    public void ClearCurrentGrabbableForSnap(Transform snapTarget, bool destroyOnSnap)
    {
        if (_currentGrabbable != null)
        {
            _currentGrabbable.SnapToTarget(snapTarget, destroyOnSnap);
            _currentGrabbable = null;

            if (dropUI != null) dropUI.SetActive(false);
        }
    }

    public void ClearInteractionState()
    {
        if (_currentGrabbable != null)
        {
            _currentGrabbable.Drop();
            _currentGrabbable = null;
        }

        currentInteractable = null;
        isInteractable = false;

        if (_currentWorldUI != null)
        {
            _currentWorldUI.HidePrompt();
            _currentWorldUI = null;
        }
    }

    private void UpdateCrosshair()
    {
        if (defaultCrosshair != null && interactableCrosshair != null)
        {
            if (isInteractable)
            {
                defaultCrosshair.gameObject.SetActive(false);
                interactableCrosshair.gameObject.SetActive(true);
            }
            else
            {
                defaultCrosshair.gameObject.SetActive(true);
                interactableCrosshair.gameObject.SetActive(false);
            }
        }
    }

    public void EvaluateHUDState()
    {
        if (hudCanvas == null) return;

        bool shouldShowHUD = !_fpsController.isInspecting
                           && !_fpsController.isInMiniGame
                           && !_fpsController.isJumpscareActive
                           && !_fpsController.isDialogueActive
                           && !PauseManager.IsGamePaused
                           && !LevelManager.IsGameOver
                           && !NoteUIController.isNoteOpen;

        if (hudCanvasGroup == null)
        {
            hudCanvasGroup = hudCanvas.GetComponent<CanvasGroup>();
            if (hudCanvasGroup == null)
            {
                hudCanvasGroup = hudCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (!hudCanvas.gameObject.activeSelf)
        {
            hudCanvas.gameObject.SetActive(true);
        }

        hudCanvasGroup.alpha = shouldShowHUD ? 1f : 0f;
        hudCanvasGroup.interactable = shouldShowHUD;
        hudCanvasGroup.blocksRaycasts = shouldShowHUD;
    }

    private int GetLayerFromMask(LayerMask mask)
    {
        int bitmask = mask.value;
        if (bitmask == 0) return gameObject.layer;

        int layerIndex = 0;
        while (bitmask > 1)
        {
            bitmask >>= 1;
            layerIndex++;
        }
        return layerIndex;
    }
}