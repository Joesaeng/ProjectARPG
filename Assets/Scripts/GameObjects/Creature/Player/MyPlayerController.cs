using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class MyPlayerController : MonoBehaviour
{
    private MyPlayerAnimator _playerAnimator;
    private MyPlayerInput _input;
    private MyPlayerMovement _playerMovement;

    private PlayerInput _playerInput;
    private GameObject _mainCamera;

    private GameObject _baseCamera;
    private GameObject _lockOnCamera;

    private GameObject _lockOnTarget;
    private CinemachineTargetGroup _cinemachineTargetGroup;

    public float LockOnMaxDistance = 20f;

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

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    private readonly float _threshold = 0.01f;

    private bool _lockOnFlag = false;
    private Action<bool> OnLockOnChanged;

    private Vector2 _lookVector;

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
        _baseCamera = GameObject.FindGameObjectWithTag("BaseCamera");
        _lockOnCamera = GameObject.FindGameObjectWithTag("LockOnCamera");
        _cinemachineTargetGroup = GameObject.Find("TargetGroup").GetComponent<CinemachineTargetGroup>();

        _playerAnimator = GetComponent<MyPlayerAnimator>();
        _playerAnimator.Init();
        _playerMovement = GetComponent<MyPlayerMovement>();

        _input.OnLockOnInput += HandleLockOn;
        _input.OnAttackInput += HandleAttack;
        _input.OnUnarmInput += HandleUnarm;
        _input.OnLookInput += (vec) => _lookVector = vec; 

        _playerMovement.Init(_mainCamera);
        MovementInit(_input);
        InitWeaponTargets();

        // TEMP
        SetEquipKatana();
        SetCamera(false);
        _lockOnTarget = GameObject.Find("Target");
    }

    private void MovementInit(MyPlayerInput input)
    {
        _playerMovement.InitInputHandler(input);
        _playerMovement.OnGroundedAnimation += () =>
        {
            _playerAnimator.SetJump(false);
            _playerAnimator.SetLanding(true);
            _playerAnimator.SetFreeFall(false);
            _playerAnimator.SetGrounded(true);
        };

        _playerMovement.OnJumpAnimation += () => _playerAnimator.SetJump(true);

        _playerMovement.OnFreeFallAnimation += () =>
        {
            _playerAnimator.SetFreeFall(true);
            _playerAnimator.SetLanding(false);
        };

        _playerMovement.OnMoveAnimation += (speed,inputX,inputY) =>
        {
            _playerAnimator.SetSpeed(speed);
            _playerAnimator.SetInputX(inputX);
            _playerAnimator.SetInputY(inputY);
        };

        _playerMovement.OnGrounded += (value) => _playerAnimator.SetGrounded(value);
        OnLockOnChanged += (value) => _playerMovement._lockOnFlag = value;
    }

    private void InitWeaponTargets()
    {
        ScabbardTarget = Util.FindChild(gameObject, "Scabbard_Target01", true).transform;
        UnarmKatanaTarget = Util.FindChild(gameObject, "katana_Targer01", true).transform;
        WeaponRTarget = Util.FindChild(gameObject, "Weapon_r", true).transform;
    }

    private void Update()
    {
        _playerMovement.OnUpdate();

        CheckDistanceBetweenLockOnTarget();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void HandleAttack()
    {
        if (!EquipWeapon && !EquipingWeapon)
            StartCoroutine(CoSetEquip());

    }

    private void HandleUnarm()
    {
        if (EquipWeapon && !EquipingWeapon)
            StartCoroutine(CoSetEquip());
    }

    private IEnumerator CoSetEquip()
    {
        if (EquipingWeapon)
            yield break;
        EquipingWeapon = true;
        _playerAnimator.SetEquiping(EquipingWeapon);
        if (EquipWeapon)
        {
            _playerAnimator.Unarm();
        }
        else
        {
            _playerAnimator.Equip();
        }

        yield return YieldCache.WaitForSeconds(0.5f);
        EquipingWeapon = false;
    }

    private void HandleLockOn()
    {
        // TODO
        // TEMP
        SetLockOn(!_lockOnFlag);
    }

    private void SetLockOn(bool lockOnFlag)
    {
        _lockOnFlag = lockOnFlag;
        OnLockOnChanged?.Invoke(_lockOnFlag);
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

        _playerAnimator.SetLockOn(_lockOnFlag);
    }

    private void CameraRotation()
    {
        // 입력이 있고 카메라 위치가 고정되지 않은 경우
        if (_lookVector.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            // 마우스를 이용할 때는 deltaTime을 곱하지 않는다
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _lookVector.x * deltaTimeMultiplier * YawSpeed;
            _cinemachineTargetPitch += _lookVector.y * deltaTimeMultiplier * PitchSpeed;
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

    // 애니메이션 이벤트
    

    public void OnLanding()
    {
        // 랜딩 후에 Has Exit Time을 하니 애니메이션이 자연스럽게 넘어가지 않아서 만듬
        _playerAnimator.SetLanding(true);
    }

    public void OnEquipKatana()
    {
        EquipWeapon = true;
        if (_playerMovement.CurMoveState == PlayerMoveState.Idle)
            _playerAnimator.SetKatanaAnimayerLayer(EquipWeapon, 0.5f);
        else
            _playerAnimator.SetKatanaAnimayerLayer(EquipWeapon, 0.1f);
        SetEquipKatana();
    }

    public void OnUnarmKatana()
    {
        EquipWeapon = false;
        if (_playerMovement.CurMoveState == PlayerMoveState.Idle)
            _playerAnimator.SetKatanaAnimayerLayer(EquipWeapon, 0.5f);
        else
            _playerAnimator.SetKatanaAnimayerLayer(EquipWeapon, 0.1f);
        SetEquipKatana();
    }

    private void SetEquipKatana()
    {
        EquipKatana.SetActive(EquipWeapon);
        UnEquipKatana.SetActive(!EquipWeapon);
        _playerAnimator.SetEquiping(false);
    }
}
