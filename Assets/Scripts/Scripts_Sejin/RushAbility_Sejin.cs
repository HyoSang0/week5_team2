using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RushAbility_Sejin : MonoBehaviour, IDamageSource
{
    // public TextMeshProUGUI dashEnergyText;
    // public float energy = 0.0f;
    // public float earnEnergy = 5.0f;
    // public float consumeEnergy = 10.0f;
    // public float maxEnergy = 30.0f;

    public float rushSpeed = 50.0f;
    public float duringTime = 0.2f;
    public float noDamageTime = 0.7f;
    [Min(1)] public int maxRushCount;
    public float coolTime = 1.0f;
    public bool isDashing = false;
    [Tooltip("현재 사용 가능한 돌진 횟수")]
    private int _usableRushCount;

    private Coroutine dashRoutine;
    private Coroutine unbeatRoutine;
    private Coroutine cooldownRoutine;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private PlayerController playerController;
    private PlayerHp playerHp;
    private PlayerAttack_Dropkick _playerAttackDropkick;

    private GameObject dashReadyEffectPrefab;
    private GameObject dashReadyEffectObject;
    [SerializeField] private Image _coolDownImage;

    public event Action OnRushStarted;
    public event Action OnRushEnded;

    public bool isRushing = false;

    private EnemyPool enemyPool;

    private PlayerFallRecovery fallRecovery;

    [Header("돌진 직격 피해량")]
    /// <summary>
    /// 돌진 중 적과 충돌했을 때 적의 피해 진입점에 전달하는 피해량.
    /// </summary>
    [SerializeField] private int _rushDamage = 20;

    [Header("증강 스탯 재계산에 사용할 기준값들")]
    /// <summary>
    /// 돌진 지속 시간
    /// </summary>
    private float _baseDuringTime;
    /// <summary>
    /// 돌진 쿨타임
    /// </summary>
    private float _baseCoolTime;
    /// <summary>
    /// 돌진 무적 시간
    /// </summary>
    private float _baseNoDamageTime;
    /// <summary>
    /// 돌진 속도
    /// </summary>
    private float _baseRushSpeed;
    /// <summary>
    /// 돌진 횟수
    /// </summary>
    private float _baseRushCount;
    // 쿨타임 진행 중에 누적된 쿨타임 감소량과 쿨타임 진행 여부
    private float _pendingCooldownReduction;
    private bool _inCooldown;

    void Awake()
    {
        // energy = 0.0f;
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        enemyPool = GameObject.Find("ObjectPool").GetComponent<EnemyPool>();
        playerHp = GetComponent<PlayerHp>();
        fallRecovery = GetComponent<PlayerFallRecovery>();
        _baseDuringTime = duringTime;
        _baseCoolTime = coolTime;
        _baseNoDamageTime = noDamageTime;
        _baseRushSpeed = rushSpeed;
        _baseRushCount = maxRushCount;
        _usableRushCount = maxRushCount;

        dashReadyEffectPrefab = Resources.Load<GameObject>("Prefabs/DashReadyEffect");
        dashReadyEffectObject = Instantiate(dashReadyEffectPrefab, transform.position, Quaternion.identity);

    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();

        // 컴포넌트 비활성화로 코루틴은 정지하지만 필드는 남으므로 참조를 명시적으로 멈추고 정리한다.
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }
        if (unbeatRoutine != null)
        {
            StopCoroutine(unbeatRoutine);
            unbeatRoutine = null;
        }

        // 돌진 이동 중 비활성화면 정상 종료와 동일하게 이동과 상태를 복구한다.
        if (isRushing)
        {
            FinishRushMovement();
        }
        // 이동 또는 쿨타임 중 정지된 경우 쿨타임을 끝난 것으로 취급하고 준비 표시를 복구한다.

        if (isDashing && dashReadyEffectObject != null)
        {
            dashReadyEffectObject.SetActive(true);
        }

        // 재활성화 후 CanDash가 막히지 않도록 돌진/쿨타임 플래그를 함께 해제한다.
        isRushing = false;
        isDashing = false;

        // 컴포넌트 비활성화로 쿨타임 코루틴이 정지하면 감소량 대기 상태를 정리한다.
        _inCooldown = false;
        _pendingCooldownReduction = 0f;
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        _playerAttackDropkick = GetComponent<PlayerAttack_Dropkick>();
        OnRushStarted += playerController.StartRush;
        OnRushEnded += playerController.EndRush;
        if (_playerAttackDropkick != null)
        {
            OnRushStarted += _playerAttackDropkick.Dropkick;
        }
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
        if (playerController != null)
        {
            OnRushStarted -= playerController.StartRush;
            OnRushEnded -= playerController.EndRush;
        }
        if (_playerAttackDropkick != null)
        {
            OnRushStarted -= _playerAttackDropkick.Dropkick;
        }
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= ApplyAugmentStats;
        }
    }

    /// <summary>
    /// PlayerStats의 Rush 관련 증강을 기준값에 적용해 duringTime, coolTime, noDamageTime, rushSpeed를 재계산한다.
    /// PlayerStats.Instance가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        duringTime = PlayerStats.Instance.Apply(StatType.RushDuration, _baseDuringTime);
        coolTime = PlayerStats.Instance.Apply(StatType.RushCooldown, _baseCoolTime);
        noDamageTime = PlayerStats.Instance.Apply(StatType.RushInvincible, _baseNoDamageTime);
        rushSpeed = PlayerStats.Instance.Apply(StatType.RushSpeed, _baseRushSpeed);
        maxRushCount = (int)PlayerStats.Instance.Apply(StatType.RushCount, _baseRushCount);
        Debug.Log($"{maxRushCount}");
    }

    // 공격 입력을 받으면 사용 가능 여부를 확인하고 드롭킥을 시작한다.
    // ctx는 입력 이벤트이며 드롭킥 상태와 플레이어 무적시간을 변경한다.
    private void StartRush(InputAction.CallbackContext ctx)
    {
        if (GamePause.IsPaused)
            return;

        if (CanDash())
        {
            OnRushStarted?.Invoke();
            isRushing = true;
            _usableRushCount--;             //돌진 횟수 감소

            if (dashRoutine != null)
                StopCoroutine(dashRoutine);

            dashRoutine = StartCoroutine(Dash_Move());

            //횟수를 소모했는데 충전 코루틴이 안 돌고 있다면 쿨타임 가동
            if (!_inCooldown)
                cooldownRoutine = StartCoroutine(DashCooldown());

            //플레이어한테 무적 상태 걸기
            playerHp.ApplyDashInvincibility(noDamageTime);
        }
    }

    // 플레이어를 전진시킨 뒤 공격 지속시간과 쿨타임을 순서대로 처리한다.
    // duringTime과 coolTime을 사용하며 이동 및 isDashing 상태를 변경한다.
    private IEnumerator Dash_Move()
    {
        isDashing = true;

        //남은 횟수가 없을 때만 준비 이펙트 끔
        if (_usableRushCount == 0)
            dashReadyEffectObject.SetActive(false);

        dashReadyEffectObject.SetActive(false);
        rb.linearVelocity = transform.forward * rushSpeed;
        rb.useGravity = false;

        //Debug.Log("No Damage Start");
        yield return new WaitForSeconds(duringTime);

        FinishRushMovement();
        isDashing = false;
    }

    /// <summary>
    /// 현재 돌진 이동을 종료하고 이동 종료 이벤트를 호출한다. 
    /// Rigidbody의 속도를 초기화하고 중력과 플레이어의 일반 이동 상태를 복구한다. 
    /// </summary>
    private void FinishRushMovement()
    {
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;

        isRushing = false;
        OnRushEnded?.Invoke();
    }

    /// <summary>
    /// 시작 시점의 coolTime을 지역 기준값으로 캡처해 그 길이만큼 돌진 쿨타임을 진행하고 준비 표시를 갱신한다.
    /// 진행 중 PlayerStats 변경으로 coolTime 필드가 바뀌어도 진행 중인 쿨타임은 영향을 받지 않고, 완료 시 돌진을 다시 사용할 수 있게 한다.
    /// </summary>
    private IEnumerator DashCooldown()
    {
        _inCooldown = true;
        //돌진 가능 횟수가 모두 충전될 때까지 반복
        while (_usableRushCount < maxRushCount)
        {
            float timeElapsed = 0f;
            float cooldownDuration = coolTime;
            //각 돌진 쿨타임 측정
            while (timeElapsed < cooldownDuration)
            {
                if (_usableRushCount == 0)
                {
                    _coolDownImage.fillAmount = cooldownDuration > 0f ? timeElapsed / cooldownDuration : 1f;
                }
                else
                {
                    _coolDownImage.fillAmount = 1f; // 1번 이상 남았으면 일단 불 들어오게 처리
                }

                timeElapsed += Time.deltaTime + _pendingCooldownReduction;
                _pendingCooldownReduction = 0f;
                yield return null;
            }

            // 쿨타임 1회전 완료 -> 횟수 1 충전
            _usableRushCount++;
            dashReadyEffectObject.SetActive(true); // 횟수가 생겼으니 이펙트 온
        }

        // 최대 횟수 도달 시 코루틴 종료
        _inCooldown = false;
        _pendingCooldownReduction = 0f;
        _coolDownImage.fillAmount = 1;
        cooldownRoutine = null;
    }

    /// <summary>
    /// 낙하 복귀를 위해 진행 중인 돌진 이동을 중단하고 쿨타임을 시작한다. 
    /// isRushing이 false이면 기존 상태를 유지하며, PlayerHp의 무적 상태는 변경하지 않는다. 
    /// </summary>
    public void InterruptRushForFall()
    {
        if (!isRushing)
        {
            return;
        }
        StopCoroutine(dashRoutine);
        FinishRushMovement();
        dashRoutine = StartCoroutine(DashCooldown());
    }

    // 진행 중인 드롭킥과 쿨타임 상태를 확인한다.
    // isDashing을 사용하며 새로운 드롭킥의 사용 가능 여부를 반환한다.
    public bool CanDash()
    {
        if (fallRecovery.IsRecovering)
        {
            return false;
        }
        if (isDashing || _usableRushCount < 1) return false;
        // if (energy < consumeEnergy || isDashing) return false;
        // energy -= consumeEnergy;
        // RefreshUI();

        return true;
    }

    /// <summary>
    /// 쿨타임 진행 중이거나 돌진 이동 중일 때 남은 쿨타임을 seconds만큼 줄이기 위해 _pendingCooldownReduction에 누적한다.
    /// 돌진 중 누적된 값은 직후 시작하는 DashCooldown의 timeElapsed에 반영되고 0으로 초기화되며, 둘 다 아니면 무시된다.
    /// </summary>
    public void ReduceCooldown(float seconds)
    {
        // 돌진 중 처치로 줄어든 양이 사라지지 않도록 쿨타임 진입 전에도 누적한다.
        if (!_inCooldown && !isRushing)
        {
            return;
        }

        _pendingCooldownReduction += seconds;
    }

    // private void RefreshUI()
    // {
    //     dashEnergyText.text = $"Energy : {energy}";
    // }

    private void OnCollisionEnter(Collision collision)
    {
        // 굴러가는 통나무 적은 태그가 NoAbsortEnemy로 나뉘어 있으므로 함께 직격 피해 대상에 포함한다.
        if ((collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("NoAbsortEnemy")) && isRushing)
        {
            Enemy enemy = collision.gameObject.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                // 직격 피해는 적의 공용 피해 진입점으로 전달해 각 적의 기존 피해/사망 처리를 따르게 한다.
                IDamageable damageTarget = enemy;
                damageTarget.TakeDamage(new DamageInfo(_rushDamage, DamageKind.RushDirect, this));
            }
        }
    }
}
