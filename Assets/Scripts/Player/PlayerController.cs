using System;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    // private CapsuleCollider collider;
    
    public float moveSpeed = 10f;
    public float rushSpeed = 20f;
    private InputSystem_Actions inputActions;
    private Rigidbody rb;

    private bool isRushing = false;

    private Vector2 moveInput;

    // 맵 밖으로 못나가도록
    Vector3 centor = Vector3.zero;
    float radius = 15f;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        // collider = GetComponent<CapsuleCollider>();
    }
    void Start()
    {
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
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
        if (!isRushing)
        {
            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
        }

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
        isRushing = true;
    }

    public void EndRush()
    {
        isRushing = false;
    }

    void LimitMovement()
    {

    }
}
