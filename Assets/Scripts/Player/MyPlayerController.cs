using System.Collections;
using Unity.Cinemachine;
using Unity.Properties;
using UnityEngine;
using UnityEngine.InputSystem;

enum MoveState
{
    Idle,
    Walk,
    Run,
    Sprint,
}

enum PlayerAnimationLayer
{
    Base,
    Katana,

    EquipAnim,
}

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class MyPlayerController : MonoBehaviour
{
    private MyPlayerInput _input;
    private PlayerInput _playerInput;
    private CharacterController _controller;
    private GameObject _mainCamera;
    private Animator _animator;

    private GameObject _baseCamera;
    private GameObject _lockOnCamera;

    private GameObject _lockOnTarget;
    private CinemachineTargetGroup _cinemachineTargetGroup;

    [Header("Player")]
    public float WalkMoveSpeed = 2.0f;
    public float RunMoveSpeed = 5.335f;
    public float SprintMoveSpeed = 7.888f;

    public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10f;
    public float DecelerationChangeRate = 5f;

    public float Gravity = -15f;

    public float JumpHeight = 1.2f;
    public float JumpTimeout = 0.5f;
    public float FallTimeout = 0.15f;

    public float LockOnMaxDistance = 20f;

    [Header("PlayerGrounded")]
    public bool IsGround = true;
    public float GroundOffset  = -0.14f;
    public float GroundRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70f;
    public float BottomClamp = -20f;
    public float CameraAngleOverride = 0f;
    public bool LockCameraPosition = false;

    [Header("Cinemachine 카메라 이동 속도")]
    [Tooltip("Vertical Speed")]
    public float YawSpeed = 1f;
    [Tooltip("Horizontal Speed")]
    public float PitchSpeed = 1f;

    [Header("PlayerWeapon")]
    public Transform ScabbardTarget;
    public Transform UnarmKatanaTarget;
    public Transform WeaponRTarget;
    public bool EquipWeapon = false;
    public bool EquipingWeapon = false;

    public GameObject EquipKatana;
    public GameObject UnEquipKatana;

    int KatanaAnimationLayer = 1;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    private readonly float _threshold = 0.01f;

    // player
    private float _moveSpeed;
    private float _animationBlendMoveSpeed;
    private float _animationBlendInputX;
    private float _animationBlendInputY;
    private float _targetRotation = 0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53f;

    private MoveState _curMoveState
    {
        get
        {
            if (_input.Move == Vector2.zero)
                return MoveState.Idle;
            if (_input.Walk)
                return MoveState.Walk;
            if (_input.Sprint)
                return MoveState.Sprint;
            return MoveState.Run;
        }
    }
    private float _targetSpeed
    {
        get
        {
            float ret;
            switch (_curMoveState)
            {
                case MoveState.Walk:
                    ret = WalkMoveSpeed;
                    break;
                case MoveState.Sprint:
                    ret = SprintMoveSpeed;
                    break;
                default:
                    ret = RunMoveSpeed;
                    break;
            }
            if (!IsGround) // 땅에 붙어있지 않을때의 타겟속도를 제한
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
                case MoveState.Walk:
                    return 0.33f;
                case MoveState.Run:
                    return 0.66f;
                case MoveState.Sprint:
                    return 1f;
                default:
                    return 0f;
            }
        }
    }

    private bool _lockOnFlag = false;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDLockOn;
    private int _animIDInputX;
    private int _animIDInputY;
    private int _animIDIsGround;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDEquip;
    private int _animIDUnarm;
    private int _animIDLanding;
    private int _animIDEquiping;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
        }
    }

    private bool _hasAnimator;
    private void Awake()
    {
        if (_mainCamera == null)
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _input = GetComponent<MyPlayerInput>();
        _playerInput = GetComponent<PlayerInput>();
        _controller = GetComponent<CharacterController>();
        _baseCamera = GameObject.FindGameObjectWithTag("BaseCamera");
        _lockOnCamera = GameObject.FindGameObjectWithTag("LockOnCamera");
        _cinemachineTargetGroup = GameObject.Find("TargetGroup").GetComponent<CinemachineTargetGroup>();

        _hasAnimator = TryGetComponent(out _animator);

        InitAnimationIDs();

        _input.OnLockOnInput += LockOnInputListner;
        _input.OnAttackInput += AttackInputListner;
        _input.OnUnarmInput += UnarmInputListner;

        InitWeaponTargets();

        // TEMP
        SetEquipKatana();
        SetCamera(false);
        _lockOnTarget = GameObject.Find("Target");
    }

    private void InitWeaponTargets()
    {
        ScabbardTarget = Util.FindChild(gameObject, "Scabbard_Target01", true).transform;
        UnarmKatanaTarget = Util.FindChild(gameObject, "katana_Targer01", true).transform;
        WeaponRTarget = Util.FindChild(gameObject, "Weapon_r", true).transform;
    }

    private void InitAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDLockOn = Animator.StringToHash("LockOn");
        _animIDInputX = Animator.StringToHash("InputX");
        _animIDInputY = Animator.StringToHash("InputY");
        _animIDIsGround = Animator.StringToHash("IsGround");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDEquip = Animator.StringToHash("Equip");
        _animIDUnarm = Animator.StringToHash("Unarm");
        _animIDLanding = Animator.StringToHash("Landing");
        _animIDEquiping = Animator.StringToHash("Equiping");
    }

    private void Update()
    {
        JumpAndGravity();
        GroundedCheck();
        Move();

        CheckDistanceBetweenLockOnTarget();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void AttackInputListner()
    {
        if (!EquipWeapon && !EquipingWeapon)
            StartCoroutine(CoSetEquip());

    }

    private void UnarmInputListner()
    {
        if (EquipWeapon && !EquipingWeapon)
            StartCoroutine(CoSetEquip());
    }

    private IEnumerator CoSetEquip()
    {
        if (EquipingWeapon)
            yield break;
        EquipingWeapon = true;
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDEquiping, EquipingWeapon);
            if (EquipWeapon)
            {
                _animator.SetTrigger(_animIDUnarm);
            }
            else
            {
                _animator.SetTrigger(_animIDEquip);
            }
        }
            
        yield return YieldCache.WaitForSeconds(0.5f);
        EquipingWeapon = false;
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundOffset, transform.position.z);

        IsGround = Physics.CheckSphere(spherePosition, GroundRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        if (_hasAnimator)
            _animator.SetBool(_animIDIsGround, IsGround);
    }

    private void JumpAndGravity()
    {
        if (IsGround)
        {
            // fall timeout timer 리셋
            _fallTimeoutDelta = FallTimeout;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDLanding, true);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // 땅에 있을 때 수직속도를 제한하며 물리적으로 안정된 상태를 유지하게 함
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.Jump && _jumpTimeoutDelta <= 0.0f)
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                    _animator.SetBool(_animIDLanding, false);
                }
            }

            _input.Jump = false;
        }

        // 수직속도에 중력을 적용하여 플레이어가 낙하할 때 시간이 지남에 따라 속도가 선형적으로 증가하도록 한다.
        // 터미널속도는 53f로 설정되어 있는데, 이는 일반적인 인간이 자유낙하 할 때의 터미널 속도이다.
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private void LockOnInputListner()
    {
        // TODO
        // TEMP
        SetLockOn(!_lockOnFlag);
    }

    private void SetLockOn(bool lockOnFlag)
    {
        _lockOnFlag = lockOnFlag;
        if (_lockOnFlag)
        {
            _cinemachineTargetGroup.AddMember(_lockOnTarget.transform, 0.9f, 1f);
        }
        else
        {
            _cinemachineTargetGroup.RemoveMember(_lockOnTarget.transform);
        }
        SetCamera(_lockOnFlag);
    }

    private void CheckDistanceBetweenLockOnTarget()
    {
        if (!_lockOnFlag || _lockOnTarget == null)
            return;

        if (Vector3.Distance(transform.position, _lockOnTarget.transform.position) > LockOnMaxDistance)
        {
            SetLockOn(false);
        }
    }

    private void SetCamera(bool lockOnFlag)
    {
        _baseCamera.SetActive(false);
        _lockOnCamera.SetActive(false);

        if (lockOnFlag)
            _lockOnCamera.SetActive(true);
        else
            _baseCamera.SetActive(true);

        if (_hasAnimator)
            _animator.SetBool(_animIDLockOn, _lockOnFlag);
    }

    private void Move()
    {
        // _curMoveState에 따른 targetSpeed 설정
        float targetSpeed = _targetSpeed;

        // Vector2의 == 연산자는 근사치 비교를 사용하기 때문에, 부동 소수점 연산에서 발생하는 작은
        // 오차의 영향을 받지 않는다. 또한 두 벡터의 크기를 비교하는 연산보다 연산 비용이 적게 든다.
        // 플레이어의 입력이 없을 때 targetSpeed 설정
        if (_input.Move == Vector2.zero)
        {
            targetSpeed = 0f;
        }

        // 플레이어의 현재 수평 속도
        float curHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = 1f;

        // targetSpeed를 가속 혹은 감속한다
        if (curHorizontalSpeed < targetSpeed - speedOffset ||
            curHorizontalSpeed > targetSpeed + speedOffset)
        {
            // 비선형 결과를 만들어서 선형적인 결과보다 더 자연스러온 속도 변화를 제공.
            // Lerp함수는 0과 1의 값으로 제한되므로, 속도를 별도로 제한할 필요가 없다.
            if (curHorizontalSpeed < targetSpeed - speedOffset)
                _moveSpeed = Mathf.Lerp(curHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            if (curHorizontalSpeed > targetSpeed + speedOffset)
                _moveSpeed = Mathf.Lerp(curHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * DecelerationChangeRate);

            // 속도를 소수점 세 자리로 반올림
            _moveSpeed = Mathf.Round(_moveSpeed * 1000f) * 0.001f; // /1000f
        }
        else
            _moveSpeed = targetSpeed;

        // 애니메이터의 블렌딩 스피드 설정
        if (_animationBlendMoveSpeed <= _targetMotionSpeed)
            _animationBlendMoveSpeed = Mathf.Lerp(_animationBlendMoveSpeed, _targetMotionSpeed, Time.deltaTime * SpeedChangeRate);
        else
            _animationBlendMoveSpeed = Mathf.Lerp(_animationBlendMoveSpeed, _targetMotionSpeed, Time.deltaTime * DecelerationChangeRate);

        if (_animationBlendMoveSpeed < 0.01f)
            _animationBlendMoveSpeed = 0f;

        // LockOn 상태에서의 애니메이션을 표현하기 위함
        _animationBlendInputX = Mathf.Lerp(_animationBlendInputX, Mathf.RoundToInt(_input.Move.x), Time.deltaTime * SpeedChangeRate);
        if (Mathf.Abs(_animationBlendInputX) < 0.01f)
            _animationBlendInputX = 0f;

        _animationBlendInputY = Mathf.Lerp(_animationBlendInputY, Mathf.RoundToInt(_input.Move.y), Time.deltaTime * SpeedChangeRate);
        if (Mathf.Abs(_animationBlendInputY) < 0.01f)
            _animationBlendInputY = 0f;

        Vector3 inputDirection = new Vector3(_input.Move.x, 0f, _input.Move.y).normalized;

        // Vector2의 != 연산자는 근사치 비교를 사용하기 때문에, 부동 소수점 연산에서 발생하는 작은
        // 오차의 영향을 받지 않는다. 또한 두 벡터의 크기를 비교하는 연산보다 연산 비용이 적게 든다.
        // 이동 입력이 있을 때 플레이어를 회전시킨다
        if (_input.Move != Vector2.zero)
        {
            // 플레이어가 이동하고자 하는 방향을 inputDirection과 Camera의 방향을 통해 계산
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                _mainCamera.transform.eulerAngles.y;

            float rotation;
            // LockOn상태이고, Sprint가 아닐 때 플레이어가 타겟의 방향을 바라보게 설정
            if (_lockOnFlag && _curMoveState != MoveState.Sprint)
            {
                // float lockOnTargetDirectionY = 
                rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _mainCamera.transform.eulerAngles.y, ref _rotationVelocity, RotationSmoothTime);
            }
            // LockOn이 아닐 때 이동방향을 바라보게 설정
            else
                rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

        // Character Controller를 이용한 이동
        _controller.Move(targetDirection.normalized * (_moveSpeed * Time.deltaTime) +
            new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);


        // 애니메이터 업데이트
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlendMoveSpeed);
            _animator.SetFloat(_animIDInputX, _animationBlendInputX);
            _animator.SetFloat(_animIDInputY, _animationBlendInputY);
        }
    }

    private void CameraRotation()
    {
        // 입력이 있고 카메라 위치가 고정되지 않은 경우
        if (_input.Look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            // 마우스를 이용할 때는 deltaTime을 곱하지 않는다
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.Look.x * deltaTimeMultiplier * YawSpeed;
            _cinemachineTargetPitch += _input.Look.y * deltaTimeMultiplier * PitchSpeed;
        }

        // 카메라 이동 반경 제한
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f)
            lfAngle += 360f;
        if (lfAngle > 360f)
            lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (IsGround)
            Gizmos.color = transparentGreen;
        else
            Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundOffset, transform.position.z),
            GroundRadius);
    }

    // 애니메이션 이벤트
    public void OnJumpImpact()
    {
        // 점프 높이와 중력 값을 사용하여 점프 높이에 도달하는 데에 필요한 초기 수직 속도 설정
        // 운동방정식에서 v² = u² + 2as를 사용. v는 최종 속도, u는 초기 속도, a는 가속도(중력), s는 점프 높이
        // 즉 초기속도는 점프 높이 * -2 * 중력 의 루트를 씌운 값
        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
    }

    public void OnLanding()
    {
        // 랜딩 후에 Has Exit Time을 하니 애니메이션이 자연스럽게 넘어가지 않아서 만듬
        _animator.SetBool(_animIDLanding, true);
    }

    public void OnEquipKatana()
    {
        EquipWeapon = true;
        if (_curMoveState == MoveState.Idle)
            StartCoroutine(CoSetAnimationLayerWeight(0.5f));
        else
            StartCoroutine(CoSetAnimationLayerWeight(0.1f));
        SetEquipKatana();
    }

    public void OnUnarmKatana()
    {
        EquipWeapon = false;
        if (_curMoveState == MoveState.Idle)
            StartCoroutine(CoSetAnimationLayerWeight(0.5f));
        else
            StartCoroutine(CoSetAnimationLayerWeight(0.1f));
        SetEquipKatana();
    }

    private IEnumerator CoSetAnimationLayerWeight(float duration)
    {
        float curLayerWeight = EquipWeapon ? 0f : 1f;
        float targetWeight = EquipWeapon ? 1f : 0f;
        float startWeight = curLayerWeight;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            curLayerWeight = Mathf.Lerp(startWeight, targetWeight, elapsedTime / duration);
            _animator.SetLayerWeight(KatanaAnimationLayer, curLayerWeight);
            yield return null;
        }
        if (_hasAnimator)
            _animator.SetLayerWeight(KatanaAnimationLayer, targetWeight);
    }

    private void SetEquipKatana()
    {
        EquipKatana.SetActive(EquipWeapon);
        UnEquipKatana.SetActive(!EquipWeapon);
        if (_hasAnimator)
            _animator.SetBool(_animIDEquiping, false);
    }
}
