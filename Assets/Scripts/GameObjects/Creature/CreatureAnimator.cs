using System.Collections.Generic;
using UnityEngine;

public interface IMyAnimator
{
    void InitAnimID();
}

[RequireComponent(typeof(Animator))]
public class CreatureAnimator : MonoBehaviour , IMyAnimator
{
    protected Animator _animator;
    protected Dictionary<string,int> _animIDDict;
    public virtual void Init()
    {
        _animator = GetComponent<Animator>();
        _animIDDict = new();
        InitAnimID();
    }

    protected void AddAnimationID(string parameterName)
    {
        _animIDDict[parameterName] = Animator.StringToHash(parameterName);
    }

    public void SetBool(string parameterName, bool value)
    {
        _animator.SetBool(_animIDDict[parameterName], value);
    }

    public void SetFloat(string parameterName, float value)
    {
        _animator.SetFloat(_animIDDict[parameterName], value);
    }

    public void SetInteger(string parameterName, int value)
    {
        _animator.SetInteger(_animIDDict[parameterName], value);
    }

    public void SetTrigger(string parameterName)
    {
        _animator.SetTrigger(_animIDDict[parameterName]);
    }

    public void SetLayerWeight(int layer, float weight)
    {
        _animator.SetLayerWeight(layer, weight);
    }

    public virtual void InitAnimID()
    {
        
    }
}
