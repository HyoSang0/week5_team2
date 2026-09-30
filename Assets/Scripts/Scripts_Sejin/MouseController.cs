using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private bool gamepadConnected = false;

    public Vector2 screenPos;
    public Vector3 worldPosition;
    public float angle;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void Start()
    {

    }
    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Look.performed += ctx => screenPos = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.performed += CheckDeviceType;
        inputActions.Player.Look.canceled += ctx => screenPos = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += CheckDeviceType;
    }

    void OnDisable()
    {
        inputActions.Player.Look.performed -= ctx => screenPos = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.performed -= CheckDeviceType;
        inputActions.Player.Look.canceled -= ctx => screenPos = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled -= CheckDeviceType;
        inputActions.Disable();
    }

    void Update()
    {
        if (gamepadConnected)
        {
            Vector2 targetDirection = inputActions.Player.Look.ReadValue<Vector2>();
            transform.rotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.y));
        }
        else
        {
            Vector2 mouseScreenPosition = inputActions.Player.Look.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
            Plane plane = new Plane(Vector3.up, Vector3.up);
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 targetDirection = ray.GetPoint(distance) - transform.position;
                transform.rotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.z));
            }
        }

    }

    private void CheckDeviceType(InputAction.CallbackContext ctx)
    {
        gamepadConnected = ctx.control.device is Gamepad;
    }
}
