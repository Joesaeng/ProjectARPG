using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    GameObject _mainCamera;
    private Vector2 _lookVector;
    [Header("Cinemachine 카메라 이동 속도")]
    [Tooltip("Vertical Speed")]
    public float YawSpeed = 0.05f;
    [Tooltip("Horizontal Speed")]
    public float PitchSpeed = 0.05f;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70f;
    public float BottomClamp = -20f;

    [Header("LockOn")]
    public bool LockCameraPosition = false;
    public float LockOnMaxDistance = 20f;
    public float LockOnRotateSpeed = 3f;
    public float LockOnRadius = 3f;


    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    private readonly float _threshold = 0.01f;

    public string[] LockOnLayerNames = {"AbleLockOn" };
    private LayerMask LockOnLayers;

    public GameObject LockOnTarget;
    public Action<bool> OnLockOnTarget;

    public void Init(MyPlayerInput input, GameObject mainCamera)
    {
        input.OnLookInput += (look) => _lookVector = look;
        input.OnLockOnInput += HandleLockOn;
        CinemachineCameraTarget = Util.FindChild(gameObject, "CinemachineTarget");
        LockOnLayers = LayerMask.GetMask(LockOnLayerNames);
        _mainCamera = mainCamera;
    }

    private void LateUpdate()
    {
        LockOnCameraRotation();
        LookCameraRotation();
        CheckLockOnTarget();
    }

    private void OnDestroy()
    {
        OnLockOnTarget = null;
    }

    private void LookCameraRotation()
    {
        // 입력이 있고 카메라 위치가 고정되지 않은 경우
        if (_lookVector.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            _cinemachineTargetYaw += _lookVector.x * YawSpeed;
            _cinemachineTargetPitch += _lookVector.y * PitchSpeed;
        }
        // 카메라 이동 반경 제한
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch ,_cinemachineTargetYaw, 0.0f);
    }

    private void LockOnCameraRotation()
    {
        if (LockOnTarget != null)
        {
            Vector3 directionToTarget = LockOnTarget.transform.position - CinemachineCameraTarget.transform.position;

            float targetYaw = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Asin(directionToTarget.y / directionToTarget.magnitude) * Mathf.Rad2Deg;

            _cinemachineTargetYaw = Mathf.LerpAngle(_cinemachineTargetYaw, targetYaw, Time.deltaTime * LockOnRotateSpeed);
            // Mathf.Asin 함수는 피치각도가 위쪽으로 올라갈수록 양수, 아래쪽으로 내려갈수록 음수가 된다.
            // 하지만 카메라의 좌표계에서는 위쪽이 양수, 아래쪽이 음수로 처리되기 때문에 각도를 역으로 적용해야 한다.
            _cinemachineTargetPitch = Mathf.LerpAngle(_cinemachineTargetPitch, -targetPitch, Time.deltaTime * LockOnRotateSpeed);
        }
    }

    private void SetLockOnTarget(GameObject target)
    {
        LockOnTarget = target;
        OnLockOnTarget?.Invoke(LockOnTarget != null);
    }

    private void HandleLockOn()
    {
        if (LockOnTarget != null)
            SetLockOnTarget(null);
        else
            SearchLockOnTarget();
    }

    private void SearchLockOnTarget()
    {
        RaycastHit hit = GetLockOnTarget();
        if (hit.collider != null)
            SetLockOnTarget(hit.collider.gameObject);
        else
            SetLockOnTarget(null);
    }

    private void CheckLockOnTarget()
    {
        if(LockOnTarget != null)
        {
            RaycastHit hit = GetLockOnTarget();
            if (hit.collider != null)
            {
                LockOnTarget = hit.collider.gameObject == LockOnTarget ? LockOnTarget : hit.collider.gameObject;
            }
            else
                LockOnTarget = null;
        }
    }

    private RaycastHit GetLockOnTarget()
    {
        Vector3 startPoint = _mainCamera.transform.position;
        Physics.SphereCast(startPoint, LockOnRadius, _mainCamera.transform.forward, out RaycastHit hit, LockOnMaxDistance, LockOnLayers, QueryTriggerInteraction.Ignore);
        return hit;
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f)
            lfAngle += 360f;
        if (lfAngle > 360f)
            lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);

        Gizmos.color = transparentGreen;
        if (_mainCamera == null)
            return;

        RaycastHit hit = GetLockOnTarget();
        Vector3 startPoint = _mainCamera.transform.position;
        if (hit.collider != null)
        {
            Gizmos.DrawRay(startPoint, _mainCamera.transform.forward * hit.distance);

            Gizmos.DrawSphere(startPoint + _mainCamera.transform.forward * hit.distance, LockOnRadius);
        }
        else
        {
            Gizmos.DrawRay(startPoint, _mainCamera.transform.forward * LockOnMaxDistance);
        }
    }
}
