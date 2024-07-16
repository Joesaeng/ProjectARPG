using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class GroundChecker : MonoBehaviour
{
    public string[] GroundLayerNames = {"Ground" };
    protected LayerMask GroundLayers;

    protected float _groundOffset;
    protected void Start()
    {
        AssignGroundLayer();
        Init();
    }
    private void AssignGroundLayer()
    {
        GroundLayers = LayerMask.GetMask(GroundLayerNames);
    }
    protected abstract void Init();
    public abstract bool IsGrounded();
}

public class CharacterControllerGroundChecker : GroundChecker
{
    private float _sphereRadius;
    protected override void Init()
    {
        CharacterController characterController = GetComponent<CharacterController>();
        if(characterController != null )
        {
            _groundOffset = characterController.height * 0.5f;
            _sphereRadius = characterController.radius;
        }
        else
        {
            Debug.LogError("CharacterController를 찾을 수 없습니다");
        }
    }

    public override bool IsGrounded()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        return Physics.CheckSphere(spherePosition, _sphereRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (IsGrounded())
            Gizmos.color = transparentGreen;
        else
            Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y, transform.position.z),
            _sphereRadius);
    }

}

public class SphereGroundChecker : GroundChecker
{
    private float _sphereRadius;
    protected override void Init()
    {
        Collider collider = GetComponent<Collider>();
        if(collider is SphereCollider sphere)
        {
            _sphereRadius = sphere.radius;
            _groundOffset = sphere.radius;
        }
        else if (collider is CapsuleCollider capsule)
        {
            _sphereRadius = capsule.radius;
            _groundOffset = capsule.height * 0.5f;
        }
        else
        {
            Debug.LogError("Collider의 타입이 Sphere 혹은 Capsule이 아닙니다");
        }
    }
    public override bool IsGrounded()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundOffset, transform.position.z);
        return Physics.CheckSphere(spherePosition, _sphereRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (IsGrounded())
            Gizmos.color = transparentGreen;
        else
            Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y, transform.position.z),
            _sphereRadius);
    }
}

public class BoxGroundChecker : GroundChecker
{
    private float _boxXSize;
    Vector3 _boxSize;

    protected override void Init()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        _groundOffset = collider.bounds.size.y * 0.5f;
        _boxXSize = collider.bounds.size.x * 0.5f;
        _boxSize = new Vector3(_boxXSize, 0.1f, _boxXSize);
    }

    public override bool IsGrounded()
    {
        Vector3 boxPosition = new Vector3(transform.position.x,transform.position.y - _groundOffset , transform.position.z);
        return Physics.CheckBox(boxPosition, _boxSize, Quaternion.identity, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (IsGrounded())
            Gizmos.color = transparentGreen;
        else
            Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawCube(
            new Vector3(transform.position.x, transform.position.y - _groundOffset, transform.position.z),
            _boxSize);
    }
}
