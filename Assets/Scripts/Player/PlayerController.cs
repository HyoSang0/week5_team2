using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public enum State
    {
        None,
        Rush,
        Charge
    }
    public float moveSpeed = 10f;
    public float rushSpeed = 20f;
    public float chargeSpeed = 5f;
    private InputSystem_Actions inputActions;
    private Rigidbody rb;

    private State state;

    private Vector2 moveInput;

    // 맵 밖으로 못나가도록
    Vector3 centor = Vector3.zero;
    float radius = 15f;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
    }
    void Start()
    {
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
        inputActions.Player.Ability_Sejin.started += _ => { SetState(State.Charge); };
        inputActions.Player.Ability_Sejin.canceled += _ => { SetState(State.None); };
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
        // Translate 기반 이동
        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
        float speed = 0f;
        switch (state)
        {
            case State.Charge:
                transform.Translate(moveDir * chargeSpeed * Time.deltaTime, Space.World);
                break;
            case State.Rush:
                break;
            default:
                transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
                break;
        }

        transform.Translate(moveDir * speed * Time.deltaTime, Space.World);

        Vector3 nowPos = transform.position;
        Vector3 centerToPlayer = nowPos - centor;
        centerToPlayer.y = 0;

        if (centerToPlayer.magnitude > radius)
        {
            Vector3 newPos = centor + centerToPlayer.normalized * radius;
            newPos.y = -0.9f;
            transform.position = newPos;
        }
    }

    public void StartRush()
    {
        state = State.Rush;
    }

    public void EndRush()
    {
        state = State.None;
    }

    public void SetState(State state)
    {
        this.state = state;
    }
}
