using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CreatureMovement : MonoBehaviour
{
    protected enum MovementState
    {
        Idle,
        Move,
    }
    protected CharacterController _characterContorller;
    protected MovementState _movementState = MovementState.Idle;
    protected Vector3 _moveDirection;
    protected float _moveSpeed;
    protected float _rotationSmoothTime = 0.12f;
    protected float _accelerationRate = 10f;
    protected float _decelerationRate = 5f;
    protected float _fallTimeout = 0.15f;

    protected float _gravity = -15f;
    protected bool _grounded = true;
    protected float _groundedOffset = -0.1f;
    protected float _groundedRadius = 0.3f;
    protected LayerMask _groundLayers;

    protected float _curMoveSpeed;
    protected float _rotationVelocity;
    protected float _verticalVelocity;
    protected float _terminalVelocity = 53f;

    protected float _fallTimeoutDelta = 0f;

    protected virtual void Start()
    {
        _characterContorller = GetComponent<CharacterController>();
    }

    protected virtual void Update()
    {
        GroundedCheck();
        ApplyGravity();
    }

    protected virtual void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);

        _grounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);
    }

    protected virtual void ApplyGravity()
    {
        if(_grounded)
        {
            _fallTimeoutDelta = _fallTimeout;

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
            _verticalVelocity += _gravity * Time.deltaTime;
        }
    }

    protected virtual void Move()
    {
        if (_movementState == MovementState.Idle)
            return;
        CalculateMoveSpeed();

        
    }

    protected virtual void CalculateMoveSpeed()
    {
        float targetSpeed = _moveSpeed;
        float curHorizontalSpeed = new Vector3(_characterContorller.velocity.x,0f,_characterContorller.velocity.z).magnitude;

        float speedOffset = 0.1f;

        // targetSpeed를 가속 혹은 감속한다
        if (curHorizontalSpeed < targetSpeed - speedOffset ||
            curHorizontalSpeed > targetSpeed + speedOffset)
        {
            // 비선형 결과를 만들어서 선형적인 결과보다 더 자연스러온 속도 변화를 제공.
            // Lerp함수는 0과 1의 값으로 제한되므로, 속도를 별도로 제한할 필요가 없다.
            if (curHorizontalSpeed < targetSpeed - speedOffset)
                _moveSpeed = Mathf.Lerp(curHorizontalSpeed, targetSpeed, Time.deltaTime * _accelerationRate);
            if (curHorizontalSpeed > targetSpeed + speedOffset)
                _moveSpeed = Mathf.Lerp(curHorizontalSpeed, targetSpeed, Time.deltaTime * _decelerationRate);

            // 속도를 소수점 세 자리로 반올림
            _curMoveSpeed = Mathf.Round(_moveSpeed * 1000f) * 0.001f; // /1000f
        }
        else
            _curMoveSpeed = targetSpeed;
    }
}
