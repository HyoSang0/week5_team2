using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LightTransport;

public class MouseController : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    public Vector2 screenPos;
    public Vector3 worldPosition;
    public float angle;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    void Start()
    {
        inputActions.Player.Look.performed += ctx => screenPos = ctx.ReadValue<Vector2>();
    }
    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {

        
        

        Vector2 mouseScreenPosition = inputActions.Player.Look.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        Plane plane = new Plane(Vector3.up, Vector3.up);
        if(plane.Raycast(ray, out float distance))
        {
            Vector3 targetDirection = ray.GetPoint(distance) - transform.position;
            transform.rotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.z));
        }
        
        
        //angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        //transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));

    }
}
