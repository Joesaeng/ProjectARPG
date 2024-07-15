using System.Collections;
using UnityEngine;

public class MyPlayerAnimator : CreatureAnimator
{
    int _katanaAnimationLayer = 1;
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

    public void Equip()
    {
        SetTrigger("Equip");
    }

    public void Unarm()
    {
        SetTrigger("Unarm");
    }

    public void SetKatanaAnimayerLayer(bool equipWeapon, float value)
    {
        StartCoroutine(CoSetKatanaAnimationLayerWeight(equipWeapon, value));
    }

    public IEnumerator CoSetKatanaAnimationLayerWeight(bool equipWeapon, float duration)
    {
        float curLayerWeight = equipWeapon ? 0f : 1f;
        float targetWeight = equipWeapon ? 1f : 0f;
        float startWeight = curLayerWeight;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            curLayerWeight = Mathf.Lerp(startWeight, targetWeight, elapsedTime / duration);
            SetLayerWeight(_katanaAnimationLayer, curLayerWeight);
            yield return null;
        }
        SetLayerWeight(_katanaAnimationLayer, curLayerWeight);
    }
}
