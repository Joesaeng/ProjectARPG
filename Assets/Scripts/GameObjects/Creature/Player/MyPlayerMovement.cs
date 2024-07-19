using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Windows;

public enum PlayerMoveState
{
    Idle,
    Walk,
    Run,
    Sprint,
}

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(GravityAndGround))]
public class MyPlayerMovement : MonoBehaviour
{
    GravityAndGround _gravityAndGround;
    CharacterController _characterController;
    private GameObject _mainCamera;

    #region Input
    private Vector2 _moveInput;
    private bool _jumpInput;
    private bool _walkInput;
    private bool _sprintInput;
    #endregion

    [Header("Player Move")]
    public float WalkMoveSpeed = 2.0f;
    public float RunMoveSpeed = 5.335f;
    public float SprintMoveSpeed = 7.888f;

    public float RotationSmoothTime = 0.12f;
    public float AccelerationRate = 10f;
    public float DecelerationChangeRate = 5f;

    public float JumpHeight = 1.2f;
    public float JumpTimeout = 0.5f;
    public float FallTimeout = 0.15f;

    private float _curMoveSpeed;
    private float _rotationVelocity;
    private float _animationBlendMoveSpeed;
    private float _animationBlendInputX;
    private float _animationBlendInputY;
    private float _targetRotation = 0f;
    public bool _lockOnFlag;
    public bool _equipWeaponFlag;
    private bool _isGround;

    private float _fallTimeoutDelta;
    private float _jumpTimeoutDelta;

    public Action OnGroundedAnimation;
    public Action OnJumpAnimation;
    public Action OnFreeFallAnimation;
    public Action<float,float,float> OnMoveAnimation;
    public Action<bool> OnGrounded;

    private PlayerMoveState _curMoveState
    {
        get
        {
            if (_moveInput == Vector2.zero)
                return PlayerMoveState.Idle;
            if (_walkInput)
                return PlayerMoveState.Walk;
            if (_sprintInput)
                return PlayerMoveState.Sprint;
            return PlayerMoveState.Run;
        }
    }
    private float _targetSpeed
    {
        get
        {
            float ret;
            switch (_curMoveState)
            {
                case PlayerMoveState.Walk:
                    ret = WalkMoveSpeed;
                    break;
                case PlayerMoveState.Sprint:
                    ret = SprintMoveSpeed;
                    break;
                default:
                    ret = RunMoveSpeed;
                    break;
            }
            if (!_gravityAndGround.IsGround) // 땅에 붙어있지 않을때의 타겟속도를 제한
                ret *= 0.5f;
            return ret;
        }
    }
    private float _targetMotionSpeed
    {
        get
        {
            switch (_curMoveState)
            {
                case PlayerMoveState.Walk:
                    return 0.33f;
                case PlayerMoveState.Run:
                    return 0.66f;
                case PlayerMoveState.Sprint:
                    return 1f;
                default:
                    return 0f;
            }
        }
    }

    public PlayerMoveState CurMoveState => _curMoveState;

    public void Init(GameObject mainCamera)
    {
        _mainCamera = mainCamera;
        _gravityAndGround = GetComponent<GravityAndGround>();
        _characterController = GetComponent<CharacterController>();
        _gravityAndGround.OnGrounded += (value) =>
        {
            _isGround = value;
            OnGrounded?.Invoke(_isGround);
        };
    }


    public void InitInputHandler(MyPlayerInput input)
    {
        input.OnMoveInput += (inputvec) => _moveInput = inputvec;
        input.OnJumpInput += (value) => _jumpInput = value;
        input.OnWalkInput += (value) => _walkInput = value;
        input.OnSprintInput += (value) => _sprintInput = value;
    }

    private void OnDestroy()
    {
        OnGroundedAnimation = null;
        OnJumpAnimation = null;
        OnFreeFallAnimation = null;
        OnMoveAnimation = null;
        OnGrounded = null;
    }

    public void OnUpdate()
    {
        JumpAndFall();
        Move();
        UpdateAnimationParameter();
        UpdateCharacterRotation();
    }

    private void JumpAndFall()
    {
        if(_isGround)
        {
            _fallTimeoutDelta = FallTimeout;

            OnGroundedAnimation?.Invoke();

            if (_jumpInput && _jumpTimeoutDelta <= 0f)
            {
                OnJumpAnimation?.Invoke();
            }

            if(_jumpTimeoutDelta >= 0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            if(_fallTimeoutDelta >= 0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                OnFreeFallAnimation?.Invoke();
            }

            _jumpInput = false;
        }
    }

    private void Move()
    {
        // _curMoveState에 따른 targetSpeed 설정
        float targetSpeed = _targetSpeed;

        // Vector2의 == 연산자는 근사치 비교를 사용하기 때문에, 부동 소수점 연산에서 발생하는 작은
        // 오차의 영향을 받지 않는다. 또한 두 벡터의 크기를 비교하는 연산보다 연산 비용이 적게 든다.
        // 플레이어의 입력이 없을 때 targetSpeed 설정
        if (_moveInput == Vector2.zero)
        {
            targetSpeed = 0f;
        }

        // 플레이어의 현재 수평 속도
        float curHorizontalSpeed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = 1f;

        // targetSpeed를 가속 혹은 감속한다
        if (curHorizontalSpeed < targetSpeed - speedOffset ||
            curHorizontalSpeed > targetSpeed + speedOffset)
        {
            // 비선형 결과를 만들어서 선형적인 결과보다 더 자연스러온 속도 변화를 제공.
            // Lerp함수는 0과 1의 값으로 제한되므로, 속도를 별도로 제한할 필요가 없다.
            if (curHorizontalSpeed < targetSpeed - speedOffset)
                _curMoveSpeed = Mathf.Lerp(curHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * AccelerationRate);
            if (curHorizontalSpeed > targetSpeed + speedOffset)
                _curMoveSpeed = Mathf.Lerp(curHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * DecelerationChangeRate);

            // 속도를 소수점 세 자리로 반올림
            _curMoveSpeed = Mathf.Round(_curMoveSpeed * 1000f) * 0.001f; // /1000f
        }
        else
            _curMoveSpeed = targetSpeed;

        Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

        // Character Controller를 이용한 이동
        _characterController.Move(targetDirection.normalized * (_curMoveSpeed * Time.deltaTime)+
            new Vector3(0f, _gravityAndGround._verticalVelocity, 0f) * Time.deltaTime);
    }

    private void UpdateAnimationParameter()
    {
        // 애니메이터의 블렌딩 스피드 설정
        if (_animationBlendMoveSpeed <= _targetMotionSpeed)
            _animationBlendMoveSpeed = Mathf.Lerp(_animationBlendMoveSpeed, _targetMotionSpeed, Time.deltaTime * AccelerationRate);
        else
            _animationBlendMoveSpeed = Mathf.Lerp(_animationBlendMoveSpeed, _targetMotionSpeed, Time.deltaTime * DecelerationChangeRate);

        if (_animationBlendMoveSpeed < 0.01f)
            _animationBlendMoveSpeed = 0f;

        // LockOn 상태에서의 애니메이션을 표현하기 위함
        _animationBlendInputX = Mathf.Lerp(_animationBlendInputX, Mathf.RoundToInt(_moveInput.x), Time.deltaTime * AccelerationRate);
        if (Mathf.Abs(_animationBlendInputX) < 0.01f)
            _animationBlendInputX = 0f;

        _animationBlendInputY = Mathf.Lerp(_animationBlendInputY, Mathf.RoundToInt(_moveInput.y), Time.deltaTime * AccelerationRate);
        if (Mathf.Abs(_animationBlendInputY) < 0.01f)
            _animationBlendInputY = 0f;

        // 애니메이터 업데이트
        OnMoveAnimation?.Invoke(_animationBlendMoveSpeed, _animationBlendInputX, _animationBlendInputY);
    }

    private void UpdateCharacterRotation()
    {
        // Vector2의 != 연산자는 근사치 비교를 사용하기 때문에, 부동 소수점 연산에서 발생하는 작은
        // 오차의 영향을 받지 않는다. 또한 두 벡터의 크기를 비교하는 연산보다 연산 비용이 적게 든다.
        // 이동 입력이 있을 때 플레이어를 회전시킨다
        if (_moveInput != Vector2.zero)
        {
            Vector3 inputDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

            // 플레이어가 이동하고자 하는 방향을 inputDirection과 Camera의 방향을 통해 계산
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                _mainCamera.transform.eulerAngles.y;

            float rotation;
            // LockOn, 무기를 equip한 상태, Sprint상태가 아닐 때 캐릭터는 카메라의 방향을 바라본다.
            if (_lockOnFlag && _equipWeaponFlag && _curMoveState != PlayerMoveState.Sprint)
            {
                rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _mainCamera.transform.eulerAngles.y, ref _rotationVelocity, RotationSmoothTime);
            }
            // LockOn이 아닐 때 이동방향을 바라보게 설정
            else
                rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }
    }

    public void OnJumpImpact()
    {
        // 점프 높이와 중력 값을 사용하여 점프 높이에 도달하는 데에 필요한 초기 수직 속도 설정
        // 운동방정식에서 v² = u² + 2as를 사용. v는 최종 속도, u는 초기 속도, a는 가속도(중력), s는 점프 높이
        // 즉 초기속도는 점프 높이 * -2 * 중력 의 루트를 씌운 값
        _gravityAndGround._verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * ConstantData.Gravity);
    }
}
