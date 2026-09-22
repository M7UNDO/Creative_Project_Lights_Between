using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public Vector2 wheelNavigate;
    public Vector2 navigateDial;

    public bool sprint;
    public bool standUp;
    public bool interact;
    public bool drop;
    public bool wheel;
    public bool toggleFlashlight;
    public bool changeBattery;
    public bool placementModeOn;
    public bool placementModeOff;
    public bool placeObject;
    public bool cancel;
    public bool close;
    public bool timedAction;
    public bool confirmDial;

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

        playerControls.Player.StandUp.performed += ctx => StandUpInput(true);
        playerControls.Player.StandUp.canceled += ctx => StandUpInput(false);

        playerControls.Player.WheelNavigate.performed += ctx => WheelNavigateInput(ctx.ReadValue<Vector2>());
        playerControls.Player.WheelNavigate.canceled += ctx => WheelNavigateInput(Vector2.zero);

        playerControls.Player.NavigateDial.performed += ctx => NavigateDialInput(ctx.ReadValue<Vector2>());
        playerControls.Player.NavigateDial.canceled += ctx => NavigateDialInput(Vector2.zero);

        playerControls.Player.Sprint.performed += ctx => SprintInput(true);
        playerControls.Player.Sprint.canceled += ctx => SprintInput(false);

        playerControls.Player.Interact.performed += ctx => InteractInput(true);
        playerControls.Player.Interact.canceled += ctx => InteractInput(false);

        playerControls.Player.Drop.performed += ctx => DropInput(true);
        playerControls.Player.Drop.canceled += ctx => DropInput(false);

        playerControls.Player.TimedAction.performed += ctx => TimedActionInput(true);
        playerControls.Player.TimedAction.canceled += ctx => TimedActionInput(false);

        playerControls.Player.Wheel.performed += ctx => WheelInput(true);
        playerControls.Player.Wheel.canceled += ctx => WheelInput(false);

        playerControls.Player.Flashlight.performed += ctx => ToggleFlashlightInput(true);
        playerControls.Player.Flashlight.canceled += ctx => ToggleFlashlightInput(false);

        playerControls.Player.ChangeBattery.performed += ctx => ChangeBatteryInput(true);
        playerControls.Player.ChangeBattery.canceled += ctx => ChangeBatteryInput(false);

        playerControls.Player.PlaceObject.performed += ctx => PlaceObjectInput(true);
        playerControls.Player.PlaceObject.canceled += ctx => PlaceObjectInput(false);

        playerControls.Player.Cancel.performed += ctx => CancelInput(true);
        playerControls.Player.Cancel.canceled += ctx => CancelInput(false);

        playerControls.Player.Close.performed += ctx => CloseInput(true);
        playerControls.Player.Close.canceled += ctx => CloseInput(false);

        playerControls.Player.ConfirmDial.performed += ctx => ConfirmDialInput(true);
        playerControls.Player.ConfirmDial.canceled += ctx => ConfirmDialInput(false);

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

    public void NavigateDialInput(Vector2 newNavigateDialDirection)
    {
        navigateDial = newNavigateDialDirection;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    public void InteractInput(bool newInteractState)
    {
        interact = newInteractState;
    }

    public void DropInput(bool newDropState)
    {
        drop = newDropState;
    }

    public void StandUpInput(bool newStandUpState)
    {
        standUp = newStandUpState;
    }

    public void WheelInput(bool newWheelState)
    {
        wheel = newWheelState;
    }

    public void ToggleFlashlightInput(bool newFlashlightState)
    {
        toggleFlashlight = newFlashlightState;
    }

    public void ChangeBatteryInput(bool newBatteryState)
    {
        changeBattery = newBatteryState;
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

    public void CloseInput(bool newCloseState)
    {
        close = newCloseState;
    }

    public void TimedActionInput(bool newTimedActionState)
    {
        timedAction = newTimedActionState;
    }

    public void ConfirmDialInput(bool newConfirmDialState)
    {
        confirmDial = newConfirmDialState;
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
        navigateDial = Vector2.zero;

        sprint = false;
        standUp = false;
        interact = false;
        drop = false;
        wheel = false;
        toggleFlashlight = false;
        changeBattery = false;
        placementModeOn = false;
        placementModeOff = false;
        placeObject = false;
        cancel = false;
        close = false;
        timedAction = false;
        confirmDial = false;
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