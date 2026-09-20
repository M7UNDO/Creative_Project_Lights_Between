using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif

public class FPSController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 4.0f;
    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 6.0f;
    [Tooltip("Rotation speed of the character")]
    public float ControllerSensitivity = 1.0f;
    public float MouseSensitvity = 10f;
    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.1f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.5f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;

    [Tooltip("How restricted the up/down look angle is when holding a heavy object")]
    public float HeldObjectTopClamp = 30.0f;
    [Tooltip("How restricted the up/down look angle is when holding a heavy object")]
    public float HeldObjectBottomClamp = -30.0f;

    [Header("Movement SFX")]
    [Space(5)]

    public bool playingFootsteps = false;
    [SerializeField] private float walkFootstepSpeed = 0.5f;
    [SerializeField] private float sprintFootstepSpeed = 0.3f;
    [SerializeField] private float currentFootstepSpeed;

    [Header("Walk Footstep Clips")]
    [SerializeField] private AudioClip[] woodWalkFootstepClips;
    [SerializeField] private AudioClip[] tileWalkFootstepClips;
    [SerializeField] private AudioClip[] carpetWalkFootstepClips;
    [SerializeField] private AudioClip[] defaultWalkFootstepClips;

    [Header("Run Footstep Clips")]
    [SerializeField] private AudioClip[] woodRunFootstepClips;
    [SerializeField] private AudioClip[] tileRunFootstepClips;
    [SerializeField] private AudioClip[] carpetRunFootstepClips;
    [SerializeField] private AudioClip[] defaultRunFootstepClips;

    [SerializeField] private AudioSource stepSFX;
    [SerializeField] private float groundRayDistance = 1.5f;

    [Header("Interaction Settings")]
    [Space(5)]
    public Canvas hudCanvas;
    [SerializeField] private GameObject dropUI;
    [SerializeField] public float interactionRange = 5f;
    [SerializeField] private LayerMask interactionLayer;

    [SerializeField] private Image defaultCrosshair;
    [SerializeField] private Image interactableCrosshair;

    public bool isInteractable = false;
    public bool isInteracting;
    private IInteractable currentInteractable;
    private IKeyInventory _keyInventory;
    private IInteractable _lastInteractable;

    [Header("Grab Settings")]
    [SerializeField] private LayerMask heldLayer;
    [SerializeField] private Transform objectGrabPointTransform;
    private ObjectGrabbable _currentGrabbable;
    public ObjectGrabbable CurrentGrabbable => _currentGrabbable;

    [Header("Sitting & Sleeping State")]
    public bool isSeated { get; private set; }
    public PlayerPoseAnchor CurrentPoseAnchor { get; private set; }
    public GameObject PosepromptUI;

    [Header("Proximity & Occlusion Settings")]
    [SerializeField] private LayerMask occlusionLayer;
    private WorldSpaceInteractableUI _currentWorldUI;

    public Transform FlashlightAnchor;

    private bool _isInspecting;
    public bool isInspecting
    {
        get => _isInspecting;
        set
        {
            if (_isInspecting != value)
            {
                _isInspecting = value;
                EvaluateHUDState();
            }
        }
    }

    private bool _isDialogueActive;
    public bool isDialogueActive
    {
        get => _isDialogueActive;
        set
        {
            if (_isDialogueActive != value)
            {
                _isDialogueActive = value;
                EvaluateHUDState();
            }
        }
    }

    private bool _isInMiniGame;
    public bool isInMiniGame
    {
        get => _isInMiniGame;
        set
        {
            if (_isInMiniGame != value)
            {
                _isInMiniGame = value;
                EvaluateHUDState();
            }
        }
    }

    private bool _isJumpscareActive;
    public bool isJumpscareActive
    {
        get => _isJumpscareActive;
        set
        {
            if (_isJumpscareActive != value)
            {
                _isJumpscareActive = value;
                EvaluateHUDState();
            }
        }
    }

    [Header("Dialogue Sensitivity Settings")]
    [SerializeField] private float dialogueSensitivityMultiplier = 0.5f;
    [SerializeField] private float sensitivityTransitionSpeed = 5.0f;
    private float _currentSensitivityMultiplier = 1.0f;

    private float _footstepTimer;
    private float _cinemachineTargetPitch;

    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    public InputHandler InputHandler => _input;
    public CharacterController Controller => _controller;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private CharacterController _controller;
    private InputHandler _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    public bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputHandler>();
        _keyInventory = GetComponent<IKeyInventory>();
        if (_keyInventory == null)
        {
            _keyInventory = GetComponentInChildren<IKeyInventory>();
        }

        _playerInput = GetComponent<PlayerInput>();

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    

    private void Update()
    {
        UpdateSensitivityMultiplier();

        if (isInMiniGame || isInspecting || isJumpscareActive || isDialogueActive || PauseManager.IsGamePaused || NoteUIController.isNoteOpen) return;

        if (isSeated)
        {
            if (_input != null && (_input.standUp || _input.cancel))
            {
                if (CurrentPoseAnchor != null)
                {
                    CurrentPoseAnchor.RequestStandUp();
                }
                _input.standUp = false;
                _input.cancel = false;
            }
        }

        ApplyGravity();
        GroundedCheck();

        if (!isSeated)
        {
            Move();
            HandleFootsteps();
        }

        DetectInteractable();
        UpdateCrosshair();
        Interact();
    }

    private void LateUpdate()
    {
        if (isInMiniGame || isInspecting || isJumpscareActive || PauseManager.IsGamePaused || NoteUIController.isNoteOpen) return;
        CameraRotation();
    }

    private void UpdateSensitivityMultiplier()
    {
        float targetMultiplier = isDialogueActive ? dialogueSensitivityMultiplier : 1.0f;
        _currentSensitivityMultiplier = Mathf.Lerp(_currentSensitivityMultiplier, targetMultiplier, Time.deltaTime * sensitivityTransitionSpeed);
    }

    public void SetSeatedState(bool seated, Transform seatPoint = null, Transform lookAtTarget = null, PlayerPoseAnchor anchor = null)
    {
        isSeated = seated;
        CurrentPoseAnchor = seated ? anchor : null;

        if (seated && seatPoint != null)
        {
            Teleport(seatPoint.position, seatPoint.rotation);

            if (lookAtTarget != null)
            {
                ForceLookAt(lookAtTarget.position);
            }
        }

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.sprint = false;
        }

        StopFootsteps();
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation()
    {
        bool isGrabInputHeld = false;
#if ENABLE_INPUT_SYSTEM
        if (_playerInput != null)
        {
            var grabAction = _playerInput.actions.FindAction("Grab");
            if (grabAction != null)
            {
                isGrabInputHeld = grabAction.IsPressed();
            }
        }
#else
        isGrabInputHeld = Input.GetKey(KeyCode.Mouse1);
#endif

        if (_currentGrabbable != null && isGrabInputHeld)
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                float pivotSensitivity = IsCurrentDeviceMouse ? 0.5f : 50f;
                float rotationSpeed = (IsCurrentDeviceMouse ? MouseSensitvity : ControllerSensitivity) * _currentSensitivityMultiplier;

                objectGrabPointTransform.Rotate(Vector3.up * _input.look.x * rotationSpeed * pivotSensitivity * deltaTimeMultiplier);
            }
            return;
        }

        if (_input.look.sqrMagnitude >= _threshold)
        {
            float rotationSpeed = (IsCurrentDeviceMouse ? MouseSensitvity : ControllerSensitivity) * _currentSensitivityMultiplier;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch += _input.look.y * rotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _input.look.x * rotationSpeed * deltaTimeMultiplier;

            float minPitch = _currentGrabbable != null ? HeldObjectBottomClamp : BottomClamp;
            float maxPitch = _currentGrabbable != null ? HeldObjectTopClamp : TopClamp;

            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, minPitch, maxPitch);
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    public void ForceLookAt(Vector3 targetPosition)
    {
        Vector3 forwardDir = targetPosition - transform.position;
        forwardDir.y = 0f;

        if (forwardDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(forwardDir);
        }

        if (_mainCamera != null)
        {
            Vector3 camToTarget = targetPosition - _mainCamera.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(camToTarget);

            float pitch = targetRotation.eulerAngles.x;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }

            _cinemachineTargetPitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
        }
    }

    private void Move()
    {
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        if (_input.move != Vector2.zero)
        {
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void HandleFootsteps()
    {
        bool isMoving = _input.move.sqrMagnitude > 0.01f;
        bool isSprinting = _input.sprint && isMoving;
        bool shouldPlayFootsteps = isMoving && Grounded;

        currentFootstepSpeed = isSprinting ? sprintFootstepSpeed : walkFootstepSpeed;

        if (shouldPlayFootsteps)
        {
            playingFootsteps = true;
            _footstepTimer -= Time.deltaTime;

            if (_footstepTimer <= 0f)
            {
                PlayFootstepsSFX();
                _footstepTimer = currentFootstepSpeed;
            }
        }
        else
        {
            playingFootsteps = false;

            if (!isMoving)
            {
                _footstepTimer = 0f;
            }
        }
    }

    public void StopFootsteps()
    {
        playingFootsteps = false;
        _footstepTimer = 0f;
    }

    public void PlayFootstepsSFX()
    {
        if (stepSFX == null) return;

        bool isSprinting = _input.sprint && _input.move.sqrMagnitude > 0.01f;

        AudioClip[] chosenClips = GetFootstepClips(isSprinting);

        if (chosenClips == null || chosenClips.Length == 0)
            return;

        int chosenStep = Random.Range(0, chosenClips.Length);

        stepSFX.pitch = Random.Range(0.9f, 1.15f);
        stepSFX.PlayOneShot(chosenClips[chosenStep]);
    }

    private AudioClip[] GetFootstepClips(bool isSprinting)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundRayDistance, GroundLayers))
        {
            if (hit.collider.CompareTag("Wood"))
            {
                return isSprinting ? woodRunFootstepClips : woodWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Tile"))
            {
                return isSprinting ? tileRunFootstepClips : tileWalkFootstepClips;
            }

            if (hit.collider.CompareTag("Carpet"))
            {
                return isSprinting ? carpetRunFootstepClips : carpetWalkFootstepClips;
            }
        }

        return isSprinting ? defaultRunFootstepClips : defaultWalkFootstepClips;
    }

    private void ApplyGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }
        }
        else
        {
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    public void DetectInteractable()
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

            bool hasInteractable = hit.collider.TryGetComponent<IInteractable>(out var interactable);
            bool hasPower = hit.collider.TryGetComponent<IToolPower>(out var powerItem);
            bool hasGrabbable = grabbable != null && _currentGrabbable == null;

            if (hasInteractable || hasPower || hasGrabbable)
            {
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
                    basePrompt = "Pick up";
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

                targetedWorldUI = hit.collider.GetComponentInChildren<WorldSpaceInteractableUI>();

                if (targetedWorldUI != null && !isInspecting)
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
            if (ui == null) continue;
            if (ui == targetedWorldUI) continue;

            float distance = Vector3.Distance(transform.position, ui.transform.position);

            if (distance <= ui.ProximityDistance)
            {
                Vector3 targetPos = ui.transform.position;
                Vector3 direction = (targetPos - origin).normalized;
                float checkDistance = Vector3.Distance(origin, targetPos);

                if (Physics.Raycast(origin, direction, out RaycastHit losHit, checkDistance, occlusionLayer))
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

    public void Interact()
    {
        if (_input.drop)
        {
            if (_currentGrabbable != null)
            {
                _currentGrabbable.Drop();
                _currentGrabbable = null;

                if (dropUI != null)
                {
                    dropUI.SetActive(false);
                }
            }

            _input.drop = false;
        }

        if (_input.interact)
        {
            if (currentInteractable != null)
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
            else if (_currentGrabbable == null)
            {
                Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer, QueryTriggerInteraction.Collide))
                {
                    ObjectGrabbable grabbable = hit.collider.GetComponentInParent<ObjectGrabbable>();
                    if (grabbable != null)
                    {
                        _currentGrabbable = grabbable;
                        _currentGrabbable.Grab(objectGrabPointTransform, GetLayerFromMask(heldLayer));
                        _input.interact = false;

                        if (dropUI != null)
                        {
                            dropUI.SetActive(true);
                        }
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

        if (dropUI != null)
        {
            dropUI.SetActive(true);
        }

    }

    public void ClearCurrentGrabbableForSnap(Transform snapTarget, bool destroyOnSnap)
    {
        if (_currentGrabbable != null)
        {
            _currentGrabbable.SnapToTarget(snapTarget, destroyOnSnap);
            _currentGrabbable = null;

            if (dropUI != null)
            {
                dropUI.SetActive(false);
            }
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

    public void UpdateCrosshair()
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

        bool shouldShowHUD = !isInspecting
                           && !isInMiniGame
                           && !isJumpscareActive
                           && !LevelManager.IsGameOver
                           && !NoteUIController.isNoteOpen;

        if (hudCanvas.gameObject.activeSelf != shouldShowHUD)
        {
            hudCanvas.gameObject.SetActive(shouldShowHUD);
        }
    }

    public void SetInspectionMode(bool inspecting)
    {
        isInspecting = inspecting;

        ClearInteractionState();
        UpdateCrosshair();

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.sprint = false;
            _input.ClearLookInput();

            _input.blockLookInput = inspecting;
        }

        if (inspecting)
        {
            StopFootsteps();
        }
    }

    public void SetDialogueMode(bool dialogue)
    {
        isDialogueActive = dialogue;

        currentInteractable = null;
        isInteractable = false;

        if(CurrentGrabbable != null)
        {
            if (dropUI != null)
            {
                dropUI.SetActive(!dialogue);
            }
        }

        

        if (_currentWorldUI != null)
        {
            _currentWorldUI.HidePrompt();
            _currentWorldUI = null;
        }

        UpdateCrosshair();

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.sprint = false;
        }

        if (dialogue)
        {
            StopFootsteps();
        }
    }

    public void SetJumpScareMode(bool inspecting)
    {
        isInspecting = inspecting;

        ClearInteractionState();
        UpdateCrosshair();

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.sprint = false;
            _input.ClearLookInput();

            _input.cursorInputForLook = !inspecting;
            _input.blockLookInput = inspecting;
        }

        if (inspecting)
        {
            StopFootsteps();
        }
    }

    public void SetMiniGameMode(bool active)
    {
        isInMiniGame = active;
        EvaluateHUDState();

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.sprint = false;
            _input.interact = false;
            _input.wheel = false;
            _input.ClearLookInput();

            _input.cursorInputForLook = !active;
            _input.blockLookInput = active;
        }

        if (active)
        {
            StopFootsteps();
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (_currentGrabbable != null)
        {
            _currentGrabbable.Drop();
            _currentGrabbable = null;
        }

        if (_controller != null) _controller.enabled = false;

        transform.position = position;

        float targetYRotation = rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0.0f, targetYRotation, 0.0f);

        _verticalVelocity = 0.0f;
        _speed = 0.0f;

        _cinemachineTargetPitch = 0.0f;
        if (CinemachineCameraTarget != null)
        {
            CinemachineCameraTarget.transform.localRotation = Quaternion.identity;
        }

        if (_controller != null) _controller.enabled = true;
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

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }
}