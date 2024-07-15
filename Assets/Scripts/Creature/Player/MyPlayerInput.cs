using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MyPlayerInput : MonoBehaviour
{
    public Vector2 Move;
    public Vector2 Look;
    public bool Jump;
    public bool Sprint;
    public bool Walk;

    public bool _cursorLocked = true;
    public bool _cursorInputForLook = true;

    public Action OnLockOnInput;
    public Action OnAttackInput;
    public Action OnUnarmInput;

    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (_cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }

    public void OnWalk(InputValue value)
    {
        WalkInput(value.isPressed);
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnLockOn(InputValue value)
    {
        LockOnInput();
    }

    public void OnAttack(InputValue value)
    {
        AttackInput();
    }

    public void OnUnarm(InputValue value)
    {
        UnarmInput();
    }

    private void MoveInput(Vector2 newMoveDirection)
    {
        Move = newMoveDirection;
    }

    private void LookInput(Vector2 newLookDirection)
    {
        Look = newLookDirection;
    }

    private void SprintInput(bool newSprintState)
    {
        Sprint = newSprintState;
    }

    private void WalkInput(bool newWalkState)
    {
        Walk = newWalkState;
    }

    private void JumpInput(bool newJumpState)
    {
        Jump = newJumpState;
    }

    private void LockOnInput()
    {
        OnLockOnInput?.Invoke();
    }

    private void AttackInput()
    {
        OnAttackInput?.Invoke();
    }

    private void UnarmInput()
    {
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
