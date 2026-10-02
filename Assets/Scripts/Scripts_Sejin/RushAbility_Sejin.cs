using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RushAbility_Sejin : MonoBehaviour
{
    public TextMeshProUGUI dashEnergyText;
    public float energy = 0.0f;
    public float earnEnergy = 5.0f;
    public float consumeEnergy = 10.0f;
    public float maxEnergy = 30.0f;
    public float rushSpeed = 50.0f;
    public float duringTime = 0.2f;
    public float noDamageTime = 0.7f;
    public float coolTime = 1.0f;
    public bool isDashing = false;

    private Coroutine dashRoutine;
    private Coroutine unbeatRoutine;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private PlayerController playerController;
    private PlayerHp playerHp;

    public UnityEvent onStartRush;
    public UnityEvent onEndRush;
    public UnityEvent onBeatable;

    public bool isRushing = false;

    private EnemyPool enemyPool;

    // 증강 스탯 재계산에 사용할 기준값들
    private float _baseConsumeEnergy;
    private float _baseCoolTime;
    private float _baseNoDamageTime;
    private float _baseRushSpeed;

    void Awake()
    {
        energy = 0.0f;
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        enemyPool = GameObject.Find("ObjectPool").GetComponent<EnemyPool>();
        playerHp = GetComponent<PlayerHp>();

        _baseConsumeEnergy = consumeEnergy;
        _baseCoolTime = coolTime;
        _baseNoDamageTime = noDamageTime;
        _baseRushSpeed = rushSpeed;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        inputActions.Player.Attack.started += StartRush;

        // Start는 씬의 모든 Awake 이후 실행되므로 여기서 구독하면 PlayerStats.Awake 순서와 무관하다.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += ApplyAugmentStats;
        }

        ApplyAugmentStats();
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= ApplyAugmentStats;
        }
    }

    /// <summary>
    /// PlayerStats의 Rush 관련 증강을 기준값에 적용해 consumeEnergy, coolTime, noDamageTime, rushSpeed를 재계산한다.
    /// PlayerStats.Instance가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        consumeEnergy = PlayerStats.Instance.Apply(StatType.RushCost, _baseConsumeEnergy);
        coolTime = PlayerStats.Instance.Apply(StatType.RushCooldown, _baseCoolTime);
        noDamageTime = PlayerStats.Instance.Apply(StatType.RushInvincible, _baseNoDamageTime);
        rushSpeed = PlayerStats.Instance.Apply(StatType.RushSpeed, _baseRushSpeed);
    }

    private void StartRush(InputAction.CallbackContext ctx)
    {
        if (AugmentSelection.IsOpen)
        {
            return;
        }

        if (CanDash())
        {
            onStartRush.Invoke();
            isRushing = true;
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
            }
            dashRoutine = StartCoroutine(Dash_Move());

            playerHp.UpdateUnBeatTime(noDamageTime);
        }
    }

    private IEnumerator Dash_Move()
    {
        isDashing = true;
        rb.linearVelocity = transform.forward * rushSpeed;
        rb.useGravity = false;
        Debug.Log("No Damage Start");
        yield return new WaitForSeconds(duringTime);
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        onEndRush.Invoke();
        isRushing = false;
        yield return new WaitForSeconds(coolTime);
        isDashing = false;
    }

    // Rush 기능을 사용 가능한 Energy 관리 체계

    // Energy 소모 함수 & 대쉬 가능 여부 반환 
    public bool CanDash()
    {
        if (energy < consumeEnergy || isDashing) return false;

        energy -= consumeEnergy;
        RefreshUI();

        return true;
    }

    /// <summary>
    /// amount만큼 대시 Energy를 추가한다. energy는 maxEnergy를 초과하지 않는다.
    /// 변경된 energy를 RefreshUI로 UI에 반영한다.
    /// </summary>
    public void AddEnergy(float amount)
    {
        energy += amount;
        if (energy > maxEnergy)
        {
            energy = maxEnergy;
        }
        RefreshUI();
    }

    // Energy 충전 이벤트 수신
    public void RegenEnergy()
    {
        energy += earnEnergy;
        if (energy > maxEnergy)
        {
            energy = maxEnergy;
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        dashEnergyText.text = $"Energy : {energy}";
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && isRushing)
        {
            Enemy enemy = collision.gameObject.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.Kill();
            }
        }
    }
}
