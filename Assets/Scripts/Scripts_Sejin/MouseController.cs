using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private bool gamepadConnected = false;
    private Vector2 lastTargetDirection = new Vector2();

    public Vector2 lookInput;
    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Look.performed += UpdateScreenPos;
        inputActions.Player.Look.performed += CheckDeviceType;

        inputActions.Player.Look.canceled += UpdateScreenPos;
        inputActions.Player.Look.canceled += CheckDeviceType;
    }

    void OnDisable()
    {
        inputActions.Player.Look.performed -= UpdateScreenPos;
        inputActions.Player.Look.performed -= CheckDeviceType;

        inputActions.Player.Look.canceled -= UpdateScreenPos;
        inputActions.Player.Look.canceled -= CheckDeviceType;
        inputActions.Disable();
    }

    void Update()
    {
        if (gamepadConnected)
        {
            ProcessGamepadLook();
        }
        else
        {
            ProcessMouseLook();
        }
    }

    private void ProcessGamepadLook()
    {
        Vector2 targetDirection = lookInput;

        if (targetDirection.magnitude < 0.01f)
        {
            targetDirection = lastTargetDirection;
        }
        transform.rotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.y));
        lastTargetDirection = targetDirection;
    }

    private void ProcessMouseLook()
    {
        Vector2 mouseScreenPosition = lookInput;
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        Plane plane = new Plane(Vector3.up, Vector3.up);
        if (plane.Raycast(ray, out float distance))
        {
            Vector3 targetDirection = ray.GetPoint(distance) - transform.position;
            transform.rotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.z));
        }
    }

    private void UpdateScreenPos(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    private void CheckDeviceType(InputAction.CallbackContext ctx)
    {
        gamepadConnected = ctx.control.device is Gamepad;
    }
}
