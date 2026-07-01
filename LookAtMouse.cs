using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class LookAtMouse : NetworkBehaviour
{
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform playerBody;

    [SerializeField]
    private InputActionReference pointActionReference;

    [SerializeField]
    private float rotationOffset = 90f;

    [SerializeField]
    private float smoothSpeed = 15f;

    [Networked]
    private float NetworkAngle { get; set; }

    private float _displayAngle;
    private InputAction _fallbackPointAction;

    public float FacingAngle => NetworkAngle;

    private InputAction PointAction =>
        pointActionReference != null ? pointActionReference.action : _fallbackPointAction;

    private void OnEnable()
    {
        if (pointActionReference != null && pointActionReference.action != null)
        {
            pointActionReference.action.Enable();
            return;
        }

        GetOrCreateFallbackPointAction().Enable();
    }

    private void OnDisable()
    {
        if (_fallbackPointAction != null)
        {
            _fallbackPointAction.Disable();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
            return;

        // คำนวณและ sync ให้ remote เห็น
        float angle = GetMouseAngle();
        NetworkAngle = angle;
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            // local อ่าน mouse ทุกเฟรมตรงๆ ไม่รอ FixedUpdate
            _displayAngle = GetMouseAngle();
        }
        else
        {
            // remote lerp ตาม NetworkAngle
            _displayAngle = Mathf.LerpAngle(
                _displayAngle,
                NetworkAngle,
                smoothSpeed * Time.deltaTime
            );
        }

        playerBody.rotation = Quaternion.Euler(0, 0, _displayAngle + rotationOffset);
    }

    private float GetMouseAngle()
    {
        Camera lookCamera = playerCamera != null ? playerCamera : Camera.main;
        if (lookCamera == null)
        {
            return _displayAngle;
        }

        Vector2 screenPosition = ReadPointInput();
        Vector3 mousePos = lookCamera.ScreenToWorldPoint(screenPosition);
        mousePos.z = 0f;

        Vector2 direction = mousePos - transform.position;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private Vector2 ReadPointInput()
    {
        InputAction pointAction = PointAction;
        return pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private InputAction GetOrCreateFallbackPointAction()
    {
        if (_fallbackPointAction != null)
        {
            return _fallbackPointAction;
        }

        _fallbackPointAction = new InputAction(
            "Point",
            InputActionType.PassThrough,
            expectedControlType: "Vector2"
        );

        _fallbackPointAction.AddBinding("<Pointer>/position");
        _fallbackPointAction.AddBinding("<Touchscreen>/primaryTouch/position");

        return _fallbackPointAction;
    }
}
