using System;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    
    public float moveSpeed = 10f;
    public float rushSpeed = 20f;
    private InputSystem_Actions inputActions;
    private Rigidbody rb;

    private bool isRushing = false;

    private Vector2 moveInput;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
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
    }

    public void StartRush()
    {
        isRushing = true;
    }

    public void EndRush()
    {
        isRushing = false;
    }
}
