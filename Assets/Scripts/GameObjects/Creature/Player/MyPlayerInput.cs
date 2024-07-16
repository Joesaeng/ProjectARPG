using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MyPlayerInput : MonoBehaviour
{
    public bool _cursorLocked = true;
    public bool _cursorInputForLook = true;

    public Action<Vector2> OnMoveInput;
    public Action<Vector2> OnLookInput;
    public Action<bool> OnJumpInput;
    public Action<bool> OnSprintInput;
    public Action<bool> OnWalkInput;

    public Action OnLockOnInput;
    public Action OnAttackInput;
    public Action OnUnarmInput;

    private void OnDestroy()
    {
        OnMoveInput = null;
        OnLookInput = null;
        OnJumpInput = null;
        OnSprintInput = null;
        OnWalkInput = null;
        OnLockOnInput = null;
        OnAttackInput = null;
        OnUnarmInput = null;
    }

    public void OnMove(InputValue value)
    {
        OnMoveInput?.Invoke(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (_cursorInputForLook)
        {
            OnLookInput?.Invoke(value.Get<Vector2>());
        }
    }

    public void OnSprint(InputValue value)
    {
        OnSprintInput?.Invoke(value.isPressed);
    }

    public void OnWalk(InputValue value)
    {
        OnWalkInput?.Invoke(value.isPressed);
    }

    public void OnJump(InputValue value)
    {
        OnJumpInput?.Invoke(value.isPressed);   
    }

    public void OnLockOn(InputValue value)
    {
        if (value.isPressed)
            OnLockOnInput?.Invoke();
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
            OnAttackInput?.Invoke();
    }

    public void OnUnarm(InputValue value)
    {
        if (value.isPressed)
            OnUnarmInput?.Invoke();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(_cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
