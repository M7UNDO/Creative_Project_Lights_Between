using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInteraction))]
[RequireComponent(typeof(PlayerFootsteps))]
[RequireComponent(typeof(PlayerStamina))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class FPSController : MonoBehaviour
{
    [Header("Player")]
    public float MoveSpeed = 4.0f;
    public float SprintSpeed = 6.0f;
    public float ControllerSensitivity = 1.0f;
    public float MouseSensitvity = 10f;
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    public float Gravity = -15.0f;

    [Space(10)]
    public float JumpTimeout = 0.1f;
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.5f;
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 90.0f;
    public float BottomClamp = -90.0f;
    public float HeldObjectTopClamp = 30.0f;
    public float HeldObjectBottomClamp = -30.0f;

    [Header("Horizontal Clamp Settings")]
    public float dialogueHorizontalClamp = 45.0f;
    public float seatedHorizontalClamp = 60.0f;

    [Header("Sitting & Sleeping State")]
    public bool isSeated { get; private set; }
    public PlayerPoseAnchor CurrentPoseAnchor { get; private set; }

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
                if (_interaction != null) _interaction.EvaluateHUDState();
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
                if (_interaction != null) _interaction.EvaluateHUDState();
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
                if (_interaction != null) _interaction.EvaluateHUDState();
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
                if (_interaction != null) _interaction.EvaluateHUDState();
            }
        }
    }

    [Header("Dialogue Sensitivity Settings")]
    [SerializeField] private float dialogueSensitivityMultiplier = 0.5f;
    [SerializeField] private float sensitivityTransitionSpeed = 5.0f;
    private float _currentSensitivityMultiplier = 1.0f;

    private float _cinemachineTargetPitch;
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private float _baseYaw;
    private float _currentRelativeYaw;

    public InputHandler InputHandler => _input;
    public CharacterController Controller => _controller;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private CharacterController _controller;
    private InputHandler _input;
    private GameObject _mainCamera;

    private PlayerInteraction _interaction;
    private PlayerFootsteps _footsteps;
    private PlayerStamina _stamina;

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
        _interaction = GetComponent<PlayerInteraction>();
        _footsteps = GetComponent<PlayerFootsteps>();
        _stamina = GetComponent<PlayerStamina>();

#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#endif

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        UpdateSensitivityMultiplier();

        if (isInMiniGame || isInspecting || isJumpscareActive || isDialogueActive || PauseManager.IsGamePaused || InventoryUI.IsInventoryOpen || NoteUIController.isNoteOpen) return;

        if (isSeated)
        {
            if (_input != null && (_input.standUp || _input.cancel))
            {
                _input.standUp = false;
                _input.cancel = false;

                if (CurrentPoseAnchor != null)
                {
                    CurrentPoseAnchor.RequestStandUp();
                }
            }
        }

        ApplyGravity();
        GroundedCheck();

        if (!isSeated)
        {
            Move();
        }
    }

    private void LateUpdate()
    {
        if (isInMiniGame || isInspecting || isJumpscareActive || PauseManager.IsGamePaused || InventoryUI.IsInventoryOpen || NoteUIController.isNoteOpen) return;
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

        if (seated)
        {
            if (seatPoint != null)
            {
                Teleport(seatPoint.position, seatPoint.rotation);

                if (lookAtTarget != null)
                {
                    ForceLookAt(lookAtTarget.position);
                }
            }
            CacheAnchorYaw();
        }

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.sprint = false;
        }

        if (_footsteps != null)
        {
            _footsteps.StopFootsteps();
        }
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold)
        {
            float rotationSpeed = (IsCurrentDeviceMouse ? MouseSensitvity : ControllerSensitivity) * _currentSensitivityMultiplier;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch += _input.look.y * rotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _input.look.x * rotationSpeed * deltaTimeMultiplier;

            float minPitch = (_interaction != null && _interaction.CurrentGrabbable != null) ? HeldObjectBottomClamp : BottomClamp;
            float maxPitch = (_interaction != null && _interaction.CurrentGrabbable != null) ? HeldObjectTopClamp : TopClamp;

            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, minPitch, maxPitch);
            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
            }

            if (isDialogueActive || isSeated)
            {
                float allowedClamp = isSeated ? seatedHorizontalClamp : dialogueHorizontalClamp;

                _currentRelativeYaw += _rotationVelocity;
                _currentRelativeYaw = Mathf.Clamp(_currentRelativeYaw, -allowedClamp, allowedClamp);

                transform.rotation = Quaternion.Euler(0.0f, _baseYaw + _currentRelativeYaw, 0.0f);
            }
            else
            {
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }
    }

    private void CacheAnchorYaw()
    {
        _baseYaw = transform.eulerAngles.y;
        _currentRelativeYaw = 0.0f;
    }

    public void ForceLookAt(Vector3 targetPosition)
    {
        Vector3 forwardDir = targetPosition - transform.position;
        forwardDir.y = 0f;

        if (forwardDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(forwardDir);
        }

        CacheAnchorYaw();

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
            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
            }
        }
    }

    public IEnumerator SmoothLookAt(Vector3 targetPosition, float duration)
    {
        Vector3 direction = targetPosition - transform.position;

        if (direction.sqrMagnitude <= 0.01f)
            yield break;

        float targetYaw = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z)).eulerAngles.y;
        Quaternion targetPlayerRotation = Quaternion.Euler(0f, targetYaw, 0f);

        Vector3 cameraDirection = targetPosition - _mainCamera.transform.position;
        Quaternion targetCameraRotation = Quaternion.LookRotation(cameraDirection);

        float targetPitch = targetCameraRotation.eulerAngles.x;

        if (targetPitch > 180f)
            targetPitch -= 360f;

        targetPitch = Mathf.Clamp(targetPitch, BottomClamp, TopClamp);

        float startYaw = transform.eulerAngles.y;
        float startPitch = _cinemachineTargetPitch;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            float yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
            float pitch = Mathf.Lerp(startPitch, targetPitch, t);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _cinemachineTargetPitch = pitch;

            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            yield return null;
        }

        transform.rotation = targetPlayerRotation;
        _cinemachineTargetPitch = targetPitch;

        if (CinemachineCameraTarget != null)
        {
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
        }

        CacheAnchorYaw();
    }

    private void Move()
    {
        bool isMoving = _input.move != Vector2.zero;
        bool canSprint = isMoving && _input.sprint && _stamina != null && _stamina.CurrentStamina > 0f;

        if (_stamina != null)
        {
            _stamina.ProcessStamina(canSprint);
        }

        float targetSpeed = canSprint ? SprintSpeed : MoveSpeed;
        if (!isMoving) targetSpeed = 0.0f;

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

        if (isMoving)
        {
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
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

    public void SetInspectionMode(bool inspecting)
    {
        isInspecting = inspecting;

        if (_interaction != null) _interaction.ClearInteractionState();

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.sprint = false;
            _input.ClearLookInput();
            _input.blockLookInput = inspecting;
        }

        if (inspecting && _footsteps != null)
        {
            _footsteps.StopFootsteps();
        }
    }

    public void SetDialogueMode(bool dialogue)
    {
        isDialogueActive = dialogue;

        if (dialogue)
        {
            CacheAnchorYaw();
        }

        if (_interaction != null) _interaction.ClearInteractionState();

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.sprint = false;
        }

        if (dialogue && _footsteps != null)
        {
            _footsteps.StopFootsteps();
        }
    }

    public void SetJumpScareMode(bool active)
    {
        isJumpscareActive = active;

        if (_interaction != null) _interaction.ClearInteractionState();

        if (_input != null)
        {
            _input.move = Vector2.zero;
            _input.look = Vector2.zero;
            _input.sprint = false;
            _input.ClearLookInput();

            _input.cursorInputForLook = !active;
            _input.blockLookInput = active;
        }

        if (active && _footsteps != null)
        {
            _footsteps.StopFootsteps();
        }
    }

    public void SetMiniGameMode(bool active)
    {
        isInMiniGame = active;

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

        if (active && _footsteps != null)
        {
            _footsteps.StopFootsteps();
        }
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (_interaction != null) _interaction.ClearInteractionState();

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