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
    [SerializeField] private Collider playerCollider;
    public PhysicsMaterial groundMaterial;
    public PhysicsMaterial airMaterial;

    private State state;

    // 흡수 ability가 활성 상태일 때 true로 유지하며, 돌진 종료 후 복귀할 상태를 결정하는 데 쓴다.
    private bool _isAbsorbing;

    private Vector2 moveInput;

    // 맵 밖으로 못나가도록
    Vector3 centor = Vector3.zero;
    float radius = 14.8f;

    // 증강 스탯 재계산에 사용할 moveSpeed의 기준값
    private float _baseMoveSpeed;
    // 증강 스탯 재계산에 사용할 chargeSpeed의 기준값
    private float _baseChargeSpeed;
    private float _temporaryMoveSpeedMultiplier = 1f;

    private PlayerFallRecovery fallRecovery;

    [Header("바닥 확인")]
    public LayerMask groundLayer;
    public bool isGrounded = true;
    private float groundCheckDistance = 1f;
    public float groundCheckRadius = 1f;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        fallRecovery = GetComponent<PlayerFallRecovery>();
        _baseMoveSpeed = moveSpeed;
        _baseChargeSpeed = chargeSpeed;
    }
    void Start()
    {
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;

        // Start는 씬의 모든 Awake 이후 실행되므로 여기서 구독하면 PlayerStats.Awake 순서와 무관하다.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += ApplyAugmentStats;
        }

        ApplyAugmentStats();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= ApplyAugmentStats;
        }
    }

    /// <summary>
    /// PlayerStats의 MoveSpeed 증강을 기준값에 적용해 일반 이동(moveSpeed)과 흡수 이동(chargeSpeed) 속도를 모두 재계산한다.
    /// PlayerStats.Instance가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        moveSpeed = PlayerStats.Instance.Apply(StatType.MoveSpeed, _baseMoveSpeed) * _temporaryMoveSpeedMultiplier;
        chargeSpeed = PlayerStats.Instance.Apply(StatType.MoveSpeed, _baseChargeSpeed) * _temporaryMoveSpeedMultiplier;
    }

    // 일시적인 이동 속도 배율을 저장하고 기존 증강 스탯과 함께 다시 계산한다.
    public void SetTemporaryMoveSpeedMultiplier(float multiplier)
    {
        _temporaryMoveSpeedMultiplier = Mathf.Max(0f, multiplier);
        ApplyAugmentStats();
    }

    void Update()
    {
        if (fallRecovery.IsRecovering)
        {
            return;
        }

        //Cast 발사 준비
        Vector3 origin = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        //발사 (바닥 확인)
        isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer);
        if (isGrounded)
        {
            //바닥일 경우 기존에 사용중인 물리 Material 사용
            playerCollider.sharedMaterial = groundMaterial;
        }
        else
        {
            //공중에 있을 때는 마찰력을 0으로 만들어 미끌어지게 함
            playerCollider.sharedMaterial = airMaterial;
        }

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

    /// <summary>
    /// 돌진 시작 이벤트로 이동 상태를 Rush로 변경한다. 흡수 활성 여부(_isAbsorbing)는 변경하지 않는다.
    /// 입력값과 반환값은 없고 state만 변경한다.
    /// </summary>
    public void StartRush()
    {
        state = State.Rush;
    }

    /// <summary>
    /// 돌진 종료 이벤트로 Rush 상태를 해제한다. 흡수가 여전히 활성이면 Charge로, 아니면 None으로 복귀한다.
    /// _isAbsorbing을 사용하며 state를 변경한다.
    /// </summary>
    public void EndRush()
    {
        state = _isAbsorbing ? State.Charge : State.None;
    }

    /// <summary>
    /// 흡수 ability의 활성 여부를 통보받아 흡수 이동 상태를 분리 관리한다.
    /// absorbing이 true면 성공 시작, false면 해제를 의미하고, 돌진 중이 아닐 때만 state를 Charge/None으로 반영한다.
    /// _isAbsorbing을 변경하며, 돌진 중에는 state를 건드리지 않는다.
    /// </summary>
    public void SetAbsorbState(bool absorbing)
    {
        _isAbsorbing = absorbing;

        if (state == State.Rush)
        {
            return;
        }

        state = absorbing ? State.Charge : State.None;
    }

    /// <summary>
    /// 외부 요청으로 이동 상태를 직접 설정한다.
    /// state를 받아 상태 머신의 현재 상태를 변경한다.
    /// </summary>
    public void SetState(State state)
    {
        this.state = state;
    }
}
