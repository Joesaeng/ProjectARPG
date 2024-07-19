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
    private CameraController _cameraController;

    private GameObject _mainCamera;

    [Header("PlayerWeapon")]
    public Transform ScabbardTarget;
    public Transform UnarmKatanaTarget;
    public Transform WeaponRTarget;
    public bool EquipWeapon = false;
    public bool EquipingWeapon = false;

    public GameObject EquipKatana;
    public GameObject UnEquipKatana;

    private bool _lockOnFlag = false;

    private Action<bool> OnLockOnChanged;
    private Action<bool> OnEquipWeaponChanged;

    private void Awake()
    {
        if (_mainCamera == null)
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        _input = GetComponent<MyPlayerInput>();

        AnimatorInit();
        MovementInit();
        CameraControllerInit();

        _input.OnAttackInput += HandleAttack;
        _input.OnUnarmInput += HandleUnarm;

        InitWeaponTargets();

        // TEMP
        SetEquipKatana();
    }

    private void OnDestroy()
    {
        OnLockOnChanged = null;
        OnEquipWeaponChanged = null;
    }

    private void AnimatorInit()
    {
        _playerAnimator = GetComponent<MyPlayerAnimator>();
        _playerAnimator.Init();
    }

    private void CameraControllerInit()
    {
        _cameraController = GetComponent<CameraController>();
        _cameraController.Init(_input, _mainCamera);
        _cameraController.OnLockOnTarget += (lockOn) =>
        {
            _lockOnFlag = lockOn;
            OnLockOnChanged?.Invoke(_lockOnFlag);
            _playerAnimator.SetLockOn(_lockOnFlag);
        };
    }

    private void MovementInit()
    {
        _playerMovement = GetComponent<MyPlayerMovement>();
        _playerMovement.Init(_mainCamera);
        _playerMovement.InitInputHandler(_input);

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

        _playerMovement.OnMoveAnimation += (speed, inputX, inputY) =>
        {
            _playerAnimator.SetSpeed(speed);
            _playerAnimator.SetInputX(inputX);
            _playerAnimator.SetInputY(inputY);
        };

        _playerMovement.OnGrounded += (value) => _playerAnimator.SetGrounded(value);
        OnLockOnChanged += (value) => _playerMovement._lockOnFlag = value;
        OnEquipWeaponChanged += (value) => _playerMovement._equipWeaponFlag = value;
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

    // 애니메이션 이벤트

    public void OnLanding()
    {
        // 랜딩 후에 Has Exit Time을 하니 애니메이션이 자연스럽게 넘어가지 않아서 만듬
        _playerAnimator.SetLanding(true);
    }

    public void OnEquipKatana()
    {
        EquipWeapon = true;
        SetEquipKatana();
    }

    public void OnUnarmKatana()
    {
        EquipWeapon = false;
        SetEquipKatana();
    }

    private void SetEquipKatana()
    {
        EquipKatana.SetActive(EquipWeapon);
        UnEquipKatana.SetActive(!EquipWeapon);
        _playerAnimator.SetOnKatana(EquipWeapon);
        _playerAnimator.SetEquiping(false);
        OnEquipWeaponChanged?.Invoke(EquipWeapon);
    }
}
