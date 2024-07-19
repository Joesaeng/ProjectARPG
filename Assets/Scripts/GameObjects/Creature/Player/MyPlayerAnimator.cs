using System.Collections;
using UnityEngine;

public class MyPlayerAnimator : CreatureAnimator
{
    public override void InitAnimID()
    {
        base.InitAnimID();
        AddAnimationID("Speed");
        AddAnimationID("LockOn");
        AddAnimationID("InputX");
        AddAnimationID("InputY");
        AddAnimationID("IsGround");
        AddAnimationID("Jump");
        AddAnimationID("FreeFall");
        AddAnimationID("Equip");
        AddAnimationID("Unarm");
        AddAnimationID("Landing");
        AddAnimationID("Equiping");
        AddAnimationID("OnKatana");
    }

    public void SetGrounded(bool value)
    {
        SetBool("IsGround", value);
    }

    public void SetJump(bool value)
    {
        SetBool("Jump", value);
    }

    public void SetLanding(bool value)
    {
        SetBool("Landing", value);
    }

    public void SetFreeFall(bool value)
    {
        SetBool("FreeFall", value);
    }

    public void SetLockOn(bool value)
    {
        SetBool("LockOn", value);
    }

    public void SetSpeed(float value)
    {
        SetFloat("Speed", value);
    }

    public void SetInputX(float value)
    {
        SetFloat("InputX", value);
    }

    public void SetInputY(float value)
    {
        SetFloat("InputY", value);
    }

    public void SetEquiping(bool value)
    {
        SetBool("Equiping", value);
    }

    public void SetOnKatana(bool value)
    {
        SetBool("OnKatana", value);
    }

    public void Equip()
    {
        SetTrigger("Equip");
    }

    public void Unarm()
    {
        SetTrigger("Unarm");
    }
}
