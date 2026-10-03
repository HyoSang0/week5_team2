using System.Collections;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RushAbility_Sejin : MonoBehaviour
{
    // public TextMeshProUGUI dashEnergyText;
    // public float energy = 0.0f;
    // public float earnEnergy = 5.0f;
    // public float consumeEnergy = 10.0f;
    // public float maxEnergy = 30.0f;

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
    private GameObject dashReadyEffect;
    private Image coolDownImage;

    public UnityEvent onStartRush;
    public UnityEvent onEndRush;
    public UnityEvent onBeatable;

    public bool isRushing = false;

    private EnemyPool enemyPool;

    // 증강 스탯 재계산에 사용할 기준값들
    private float _baseDuringTime;
    private float _baseCoolTime;
    private float _baseNoDamageTime;
    private float _baseRushSpeed;

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

        _baseDuringTime = duringTime;
        _baseCoolTime = coolTime;
        _baseNoDamageTime = noDamageTime;
        _baseRushSpeed = rushSpeed;
        coolDownImage = GameObject.Find("Fill").GetComponent<Image>();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();

        // 컴포넌트 비활성화로 쿨타임 코루틴이 정지하면 감소량 대기 상태를 정리한다.
        _inCooldown = false;
        _pendingCooldownReduction = 0f;
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
    }

    // 공격 입력을 받으면 사용 가능 여부를 확인하고 드롭킥을 시작한다.
    // ctx는 입력 이벤트이며 드롭킥 상태와 플레이어 무적시간을 변경한다.
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
            playerHp.UpdateUnBeatTime(noDamageTime, false);
        }
    }

    // 플레이어를 전진시킨 뒤 공격 지속시간과 쿨타임을 순서대로 처리한다.
    // duringTime과 coolTime을 사용하며 이동 및 isDashing 상태를 변경한다.
    private IEnumerator Dash_Move()
    {
        isDashing = true;
        _inCooldown = false;
        _pendingCooldownReduction = 0f;
        // dashReadyEffect.SetActive(false);
        rb.linearVelocity = transform.forward * rushSpeed;
        rb.useGravity = false;
        Debug.Log("No Damage Start");

        yield return new WaitForSeconds(duringTime);

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        onEndRush.Invoke();
        isRushing = false;
        float timeElapsed = 0f;
        _inCooldown = true;
        while (timeElapsed < coolTime)
        {
            coolDownImage.fillAmount = timeElapsed / coolTime;
            timeElapsed += Time.deltaTime + _pendingCooldownReduction;
            _pendingCooldownReduction = 0f;
            yield return null;
        }
        _inCooldown = false;
        _pendingCooldownReduction = 0f;
        coolDownImage.fillAmount = 1;
        // dashReadyEffect.transform.position = transform.position;
        // dashReadyEffect.SetActive(true);
        isDashing = false;
    }

    // 진행 중인 드롭킥과 쿨타임 상태를 확인한다.
    // isDashing을 사용하며 새로운 드롭킥의 사용 가능 여부를 반환한다.
    public bool CanDash()
    {
        if (isDashing) return false;
        // if (energy < consumeEnergy || isDashing) return false;
        // energy -= consumeEnergy;
        // RefreshUI();

        return true;
    }

    // 씬에 남아 있는 흡수 도착 이벤트를 수신한다.
    // 입력과 반환값은 없으며 자원 상태를 변경하지 않는다.
    public void RegenEnergy()
    {
        // energy += earnEnergy;
        // if (energy > maxEnergy)
        // {
        //     energy = maxEnergy;
        // }
        // RefreshUI();
    }

    /// <summary>
    /// 진행 중인 드롭킥 쿨타임에서만 남은 쿨타임을 seconds만큼 줄이기 위해 _pendingCooldownReduction에 누적한다.
    /// 누적된 값은 Dash_Move의 쿨타임 루프에서 timeElapsed에 반영되고 0으로 초기화되며, 쿨타임 중이 아니면 무시된다.
    /// </summary>
    public void ReduceCooldown(float seconds)
    {
        if (!_inCooldown)
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