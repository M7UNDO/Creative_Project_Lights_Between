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
    public bool cycleFlashlightCookie;
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

#if ENABLE_INPUT_SYSTEM
    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook && !blockLookInput)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnWheelNavigate(InputValue value)
    {
        WheelNavigateInput(value.Get<Vector2>());
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }

    public void OnInteract(InputValue value)
    {
        InteractInput(value.isPressed);
    }

    public void OnWheel(InputValue value)
    {
        WheelInput(value.isPressed);
    }

    public void OnToggleFlashlight(InputValue value)
    {
        ToggleFlashlightInput(value.isPressed);
    }

    public void OnCycleFlashlightCookie(InputValue value)
    {
        CycleFlashlightCookieInput(value.isPressed);
    }

    public void OnEnterPlacementMode(InputValue value)
    {
        PlacementModeOnInput(value.isPressed);
    }

    public void OnExitPlacementMode(InputValue value)
    {
        PlacementModeOffInput(value.isPressed);
    }

    public void OnPlaceObject(InputValue value)
    {
        PlaceObjectInput(value.isPressed);
    }

    public void OnCancel(InputValue value)
    {
        CancelInput(value.isPressed);
    }

    public void OnTimedAction(InputValue value)
    {
        TimedActionInput(value.isPressed);
    }
#endif

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

    public void CycleFlashlightCookieInput(bool newCycleState)
    {
        cycleFlashlightCookie = newCycleState;
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
        cycleFlashlightCookie = false;
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