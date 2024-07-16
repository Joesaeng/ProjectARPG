using System;
using UnityEngine;

// 중력의 적용과 땅과의 접지상태를 관리
public class GravityAndGround : MonoBehaviour
{
    private bool _isGround;
    public bool IsGround => _isGround;

    public float _verticalVelocity;

    private GroundChecker _groundChecker;

    public Action<bool> OnGrounded;

    private void Start()
    {
        if (TryGetComponent(out Collider collider))
            if (collider is SphereCollider || collider is CapsuleCollider)
            {
                _groundChecker = gameObject.GetOrAddComponent<SphereGroundChecker>();
            }
            else if (collider is BoxCollider)
            {
                _groundChecker = gameObject.GetOrAddComponent<BoxGroundChecker>();
            }
        else if(TryGetComponent(out CharacterController characterController))
            {
                _groundChecker = gameObject.GetOrAddComponent<CharacterControllerGroundChecker>();
            }
        else
            {
                Debug.LogError("GroundChecker를 초기화하지 못했습니다.");
            }

    }

    private void Update()
    {
        GroundCheck();
        ApplyGravity();
    }

    private void OnDestroy()
    {
        OnGrounded = null;
    }

    private void GroundCheck()
    {
        _isGround = _groundChecker != null && _groundChecker.IsGrounded();
        OnGrounded?.Invoke(_isGround);
    }

    private void ApplyGravity()
    {
        if (IsGround)
        {
            // 땅에 있을 때 수직속도를 제한하며 물리적으로 안정된 상태를 유지하게 함
            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;
        }
        // 수직속도에 중력을 적용하여 플레이어가 낙하할 때 시간이 지남에 따라 속도가 선형적으로 증가하도록 한다.
        // 터미널속도는 53f로 설정되어 있는데, 이는 일반적인 인간이 자유낙하 할 때의 터미널 속도이다.
        if (_verticalVelocity < ConstantData.TerminalVelocity)
        {
            _verticalVelocity += ConstantData.Gravity * Time.deltaTime;
        }
    }

    
}
