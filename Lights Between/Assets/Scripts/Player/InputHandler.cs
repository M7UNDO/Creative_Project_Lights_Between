using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public Vector2 wheelNavigate;

    public bool jump;
    public bool sprint;
    public bool interact;
    public bool wheel;
    public bool toggleFlashlight;
    public bool placementModeOn;
    public bool placementModeOff;
    public bool placeObject;
    public bool cancel;
    public bool timedAction;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;
    public bool blockLookInput;

    private PlayerControls playerControls;

    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Player.Move.performed += ctx => MoveInput(ctx.ReadValue<Vector2>());
        playerControls.Player.Move.canceled += ctx => MoveInput(Vector2.zero);

        playerControls.Player.Look.performed += ctx => { if (cursorInputForLook && !blockLookInput) LookInput(ctx.ReadValue<Vector2>()); };
        playerControls.Player.Look.canceled += ctx => LookInput(Vector2.zero);

        playerControls.Player.WheelNavigate.performed += ctx => WheelNavigateInput(ctx.ReadValue<Vector2>());
        playerControls.Player.WheelNavigate.canceled += ctx => WheelNavigateInput(Vector2.zero);

        playerControls.Player.Jump.performed += ctx => JumpInput(true);
        playerControls.Player.Jump.canceled += ctx => JumpInput(false);

        playerControls.Player.Sprint.performed += ctx => SprintInput(true);
        playerControls.Player.Sprint.canceled += ctx => SprintInput(false);

        playerControls.Player.Interact.performed += ctx => InteractInput(true);
        playerControls.Player.Interact.canceled += ctx => InteractInput(false);

        playerControls.Player.TimedAction.performed += ctx => TimedActionInput(true);
        playerControls.Player.TimedAction.canceled += ctx => TimedActionInput(false);

        playerControls.Player.Wheel.performed += ctx => WheelInput(true);
        playerControls.Player.Wheel.canceled += ctx => WheelInput(false);

        playerControls.Player.Flashlight.performed += ctx => ToggleFlashlightInput(true);
        playerControls.Player.Flashlight.canceled += ctx => ToggleFlashlightInput(false);

        playerControls.Player.PlaceObject.performed += ctx => PlaceObjectInput(true);
        playerControls.Player.PlaceObject.canceled += ctx => PlaceObjectInput(false);

        playerControls.Player.Cancel.performed += ctx => CancelInput(true);
        playerControls.Player.Cancel.canceled += ctx => CancelInput(false);

        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void WheelNavigateInput(Vector2 newWheelNavigateDirection)
    {
        wheelNavigate = newWheelNavigateDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    public void InteractInput(bool newInteractState)
    {
        interact = newInteractState;
    }

    public void WheelInput(bool newWheelState)
    {
        wheel = newWheelState;
    }

    public void ToggleFlashlightInput(bool newFlashlightState)
    {
        toggleFlashlight = newFlashlightState;
    }

    public void PlacementModeOnInput(bool newPlacementState)
    {
        placementModeOn = newPlacementState;
    }

    public void PlacementModeOffInput(bool newPlacementState)
    {
        placementModeOff = newPlacementState;
    }

    public void PlaceObjectInput(bool newPlaceObjectState)
    {
        placeObject = newPlaceObjectState;
    }

    public void CancelInput(bool newCancelState)
    {
        cancel = newCancelState;
    }

    public void TimedActionInput(bool newTimedActionState)
    {
        timedAction = newTimedActionState;
    }

    public void ClearLookInput()
    {
        look = Vector2.zero;
    }

    public void ResetInputValues()
    {
        move = Vector2.zero;
        look = Vector2.zero;
        wheelNavigate = Vector2.zero;

        jump = false;
        sprint = false;
        interact = false;
        wheel = false;
        toggleFlashlight = false;
        placementModeOn = false;
        placementModeOff = false;
        placeObject = false;
        cancel = false;
        timedAction = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}