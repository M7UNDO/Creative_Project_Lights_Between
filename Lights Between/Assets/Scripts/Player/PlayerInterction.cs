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
    private ObjectGrabbable _hoveredGrabbable;
    public ObjectGrabbable CurrentGrabbable => _currentGrabbable;

    private InputHandler _input;
    private FPSController _fpsController;
    private GameObject _mainCamera;
    private KeroseneLampScript _currentKeroseneLamp;

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
        if (_fpsController.isInMiniGame || _fpsController.isInspecting || _fpsController.isJumpscareActive || _fpsController.isDialogueActive || PauseManager.IsGamePaused || NoteUIController.isNoteOpen || DrawingUIController.isDrawingOpen)
        {
            HideActiveWorldUI();
            return;
        }

        DetectInteractable();
        UpdateCrosshair();
        Interact();
    }

    private void DetectInteractable()
    {
        KeroseneLampScript previousLamp = _currentKeroseneLamp;
        _currentKeroseneLamp = null;

        currentInteractable = null;
        _hoveredGrabbable = null;
        isInteractable = false;

        WorldSpaceInteractableUI targetedWorldUI = null;
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer, QueryTriggerInteraction.Collide))
        {
            if (IsPathClear(ray.origin, hit))
            {
                targetedWorldUI = ProcessInteractableHit(hit);
            }
        }

        if (previousLamp != null && previousLamp != _currentKeroseneLamp)
        {
            previousLamp.HidePower();
        }

        HandleLastInteractableTransition();
        UpdateUnfocusedWorldUIs(targetedWorldUI);

        _currentWorldUI = targetedWorldUI;
    }

    private bool IsPathClear(Vector3 origin, RaycastHit targetHit)
    {
        Vector3 direction = targetHit.point - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit occlusionHit, distance, occlusionLayer, QueryTriggerInteraction.Ignore))
        {
            if (occlusionHit.collider != targetHit.collider && !occlusionHit.transform.IsChildOf(targetHit.transform))
            {
                return false;
            }
        }

        return true;
    }

    private WorldSpaceInteractableUI ProcessInteractableHit(RaycastHit hit)
    {
        InteractionData interaction = GetInteractionData(hit);

        if (!interaction.IsValid)
        {
            return null;
        }

        if (!IsInteractionAllowed(interaction))
        {
            return null;
        }

        ApplyInteractionState(interaction);

        InteractionResult result = DetermineInteraction(hit.collider, interaction);
        isInteractable = result.CanInteract;

        WorldSpaceInteractableUI targetedWorldUI = FindTargetedUI(hit.collider, interaction);

        if (targetedWorldUI != null && !_fpsController.isInspecting)
        {
            targetedWorldUI.ShowPrompt(result.Prompt, true, result.CanInteract);
        }

        return targetedWorldUI;
    }

    private InteractionData GetInteractionData(RaycastHit hit)
    {
        ObjectGrabbable hitGrabbable = hit.collider.GetComponentInParent<ObjectGrabbable>();

        if (_currentGrabbable != null && hitGrabbable == _currentGrabbable)
        {
            return new InteractionData();
        }

        InteractionData data = new InteractionData
        {
            Interactable = hit.collider.GetComponentInParent<IInteractable>(),
            Power = hit.collider.GetComponent<IToolPower>(),
            Grabbable = (_currentGrabbable == null && hitGrabbable != null && hitGrabbable.enabled) ? hitGrabbable : null
        };

        if (data.Interactable is MonoBehaviour behaviour && !behaviour.enabled)
        {
            data.Interactable = null;
        }

        return data;
    }

    private bool IsInteractionAllowed(InteractionData interaction)
    {
        if (interaction.Interactable is PlayerPoseAnchor)
        {
            if (_currentGrabbable != null || _fpsController.isSeated) return false;
        }

        if (interaction.Interactable is ToolPickup toolPickup)
        {
            if (!toolPickup.IsPickupUnlocked()) return false;
        }

        if (interaction.Interactable is SnapZone snapZone)
        {
            if (!snapZone.IsSnapZoneUnlocked()) return false;
        }

        if (interaction.Interactable is WallTelephoneTrigger wallPhone)
        {
            if (!wallPhone.IsTelephoneUnlocked()) return false;
        }

        if (interaction.Interactable is PhoneScript cellPhone)
        {
            if (!cellPhone.IsPhoneUnlocked()) return false;
        }

        if (interaction.HasGrabbable && _fpsController.isSeated)
        {
            return false;
        }

        return true;
    }

    private void ApplyInteractionState(InteractionData interaction)
    {
        if (interaction.HasInteractable)
        {
            currentInteractable = interaction.Interactable;
        }

        if (interaction.HasGrabbable)
        {
            _hoveredGrabbable = interaction.Grabbable;
        }
    }

    private InteractionResult DetermineInteraction(Collider hitCollider, InteractionData interaction)
    {
        InteractionResult result = new InteractionResult
        {
            Prompt = "",
            CanInteract = true
        };

        if (hitCollider.TryGetComponent<SnapZone>(out SnapZone snapZone))
        {
            if (!snapZone.IsSnapZoneUnlocked())
            {
                result.Prompt = string.Empty;
                result.CanInteract = false;
            }
            else
            {
                result.Prompt = snapZone.GetPromptMessage(_currentGrabbable);
                result.CanInteract = snapZone.CanSnap(_currentGrabbable);
            }
        }
        else if (hitCollider.TryGetComponent<WallTelephoneTrigger>(out WallTelephoneTrigger wallPhone))
        {
            result.Prompt = wallPhone.IsTelephoneUnlocked() ? wallPhone.PromptMessage : string.Empty;
            result.CanInteract = wallPhone.IsTelephoneUnlocked();
        }
        else if (hitCollider.TryGetComponent<PhoneScript>(out PhoneScript cellPhone))
        {
            result.Prompt = cellPhone.IsPhoneUnlocked() ? cellPhone.PromptMessage : string.Empty;
            result.CanInteract = cellPhone.IsPhoneUnlocked();
        }
        else if (hitCollider.TryGetComponent<PlayerPoseAnchor>(out PlayerPoseAnchor poseAnchor))
        {
            if (_fpsController.isSeated)
            {
                result.Prompt = string.Empty;
                result.CanInteract = false;
            }
            else
            {
                result.Prompt = poseAnchor.GetPromptMessage();
                result.CanInteract = poseAnchor.IsAnchorUnlocked();
            }
        }
        else if (hitCollider.TryGetComponent<DoorScript>(out DoorScript doorScript))
        {
            doorScript.UpdatePromptMessage(_keyInventory);
            result.Prompt = doorScript.promptMessage;
            result.CanInteract = doorScript.CanInteract(_keyInventory);
        }
        else if (hitCollider.TryGetComponent<GeneratorPowerSystem>(out GeneratorPowerSystem generator))
        {
            result.Prompt = generator.promptMessage;
            result.CanInteract = generator.CanInteract();
        }
        else if (hitCollider.GetComponentInParent<BarricadePlankScript>() != null)
        {
            BarricadePlankScript plank = hitCollider.GetComponentInParent<BarricadePlankScript>();
            result.Prompt = plank.promptMessage;
            result.CanInteract = plank.CanInteract();
        }
        else if (hitCollider.TryGetComponent<KeroseneLampScript>(out KeroseneLampScript kerosene))
        {
            _currentKeroseneLamp = kerosene;
            _currentKeroseneLamp.DisplayPower();
        }
        else if (hitCollider.TryGetComponent<ToolPickup>(out ToolPickup pickup))
        {
            result.Prompt = pickup.PromptMessage;
        }
        else if (hitCollider.TryGetComponent<ElectricBoxBreaker>(out ElectricBoxBreaker electricBox))
        {
            result.Prompt = electricBox.promptMessage;
        }
        else if (hitCollider.TryGetComponent<BaseOpenable>(out BaseOpenable openable))
        {
            result.Prompt = openable.promptMessage;
        }
        else if (interaction.HasGrabbable)
        {
            result.Prompt = "Grab";
        }
        else if (interaction.HasInteractable)
        {
            result.Prompt = "Interact";
        }

        if (interaction.HasPower && interaction.Power.UsesPower)
        {
            int powerPercent = Mathf.FloorToInt((interaction.Power.CurrentPower / interaction.Power.MaxPower) * 100f);

            if (!string.IsNullOrEmpty(result.Prompt))
            {
                result.Prompt = $"{result.Prompt} {powerPercent}%";
            }
            else
            {
                result.Prompt = $"Paraffin Lamp {powerPercent}%";
            }
        }

        return result;
    }

    private WorldSpaceInteractableUI FindTargetedUI(Collider hitCollider, InteractionData interaction)
    {
        Transform interactionRoot = hitCollider.transform;

        if (interaction.HasInteractable && interaction.Interactable is MonoBehaviour rootMb)
        {
            interactionRoot = rootMb.transform;
        }
        else if (interaction.HasGrabbable)
        {
            interactionRoot = interaction.Grabbable.transform;
        }

        WorldSpaceInteractableUI[] nestedUIs = interactionRoot.GetComponentsInChildren<WorldSpaceInteractableUI>(true);

        foreach (WorldSpaceInteractableUI ui in nestedUIs)
        {
            Transform closestOwner = GetClosestOwner(ui);
            if (closestOwner == interactionRoot)
            {
                return ui;
            }
        }

        return null;
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
                    ObjectGrabbable itemToSnap = _currentGrabbable;

                    if (snapZone.TrySnapItem(itemToSnap, out Transform snapTarget))
                    {
                        ClearCurrentGrabbableForSnap(snapTarget, snapZone.DestroyObjectOnSnap);
                    }

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
                else if (currentInteractable is KeroseneLampScript kerosene)
                {
                    kerosene.Interact();
                }
                else if (currentInteractable is BarricadePlankScript plank)
                {
                    plank.Interact();
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
            else if (_currentGrabbable == null && !_fpsController.isSeated && _hoveredGrabbable != null)
            {
                _currentGrabbable = _hoveredGrabbable;
                _currentGrabbable.Grab(objectGrabPointTransform, GetLayerFromMask(heldLayer));
                _input.interact = false;

                if (dropUI != null) dropUI.SetActive(true);
            }
        }
        else
        {
            if (currentInteractable is DoorScript door)
            {
                door.CancelInteract();
            }

            if (currentInteractable is KeroseneLampScript kerosene)
            {
                kerosene.CancelInteract();
            }

            if (currentInteractable is BarricadePlankScript plank)
            {
                plank.CancelInteract();
            }
        }
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

    private Transform GetClosestOwner(WorldSpaceInteractableUI ui)
    {
        IInteractable parentInteractable = ui.GetComponentInParent<IInteractable>();
        if (parentInteractable != null && parentInteractable is MonoBehaviour pMb)
        {
            return pMb.transform;
        }

        ObjectGrabbable parentGrabbable = ui.GetComponentInParent<ObjectGrabbable>();
        if (parentGrabbable != null)
        {
            return parentGrabbable.transform;
        }

        return null;
    }

    private void HandleLastInteractableTransition()
    {
        if (currentInteractable != _lastInteractable)
        {
            if (_lastInteractable is DoorScript lastDoor)
            {
                lastDoor.CancelInteract();
            }
            else if (_lastInteractable is KeroseneLampScript lastLamp)
            {
                lastLamp.CancelInteract();
            }
            else if (_lastInteractable is BarricadePlankScript lastPlank)
            {
                lastPlank.CancelInteract();
            }
        }

        _lastInteractable = currentInteractable;
    }

    private void UpdateUnfocusedWorldUIs(WorldSpaceInteractableUI targetedWorldUI)
    {
        Vector3 origin = _mainCamera.transform.position;

        foreach (var ui in WorldSpaceInteractableUI.AllInteractableUIs)
        {
            if (ui == null || !ui.enabled || !ui.gameObject.activeInHierarchy || ui == targetedWorldUI)
            {
                continue;
            }

            if (!IsUIUnlocked(ui))
            {
                ui.HidePrompt();
                continue;
            }

            float distance = Vector3.Distance(transform.position, ui.transform.position);

            if (distance <= ui.ProximityDistance)
            {
                CheckUIOcclusion(ui, origin);
            }
            else
            {
                ui.HidePrompt();
            }
        }
    }

    private bool IsUIUnlocked(WorldSpaceInteractableUI ui)
    {
        ToolPickup parentPickup = ui.GetComponentInParent<ToolPickup>();
        if (parentPickup != null && !parentPickup.IsPickupUnlocked()) return false;

        SnapZone parentSnapZone = ui.GetComponentInParent<SnapZone>();
        if (parentSnapZone != null && !parentSnapZone.IsSnapZoneUnlocked()) return false;

        WallTelephoneTrigger parentWallPhone = ui.GetComponentInParent<WallTelephoneTrigger>();
        if (parentWallPhone != null && !parentWallPhone.IsTelephoneUnlocked()) return false;

        PhoneScript parentCellPhone = ui.GetComponentInParent<PhoneScript>();
        if (parentCellPhone != null && !parentCellPhone.IsPhoneUnlocked()) return false;

        PlayerPoseAnchor parentAnchor = ui.GetComponentInParent<PlayerPoseAnchor>();
        if (parentAnchor != null && !parentAnchor.IsAnchorUnlocked()) return false;

        return true;
    }

    private void CheckUIOcclusion(WorldSpaceInteractableUI ui, Vector3 origin)
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
        currentInteractable = null;
        _hoveredGrabbable = null;
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
                           && !NoteUIController.isNoteOpen
                           && !DrawingUIController.isDrawingOpen;

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

    private class InteractionData
    {
        public IInteractable Interactable;
        public ObjectGrabbable Grabbable;
        public IToolPower Power;

        public bool HasInteractable => Interactable != null;
        public bool HasGrabbable => Grabbable != null;
        public bool HasPower => Power != null;
        public bool IsValid => HasInteractable || HasPower || HasGrabbable;
    }

    private class InteractionResult
    {
        public string Prompt;
        public bool CanInteract;
    }
}