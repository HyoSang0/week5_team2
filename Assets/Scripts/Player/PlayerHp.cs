using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class PlayerHp : MonoBehaviour, IDamageable, IHealable
{
    private const float HIT_INVINCIBILITY_SECONDS = 2f;

    Rigidbody rb;
    [SerializeField] GameManager gameManager;
    [SerializeField] private Volume volume;
    [SerializeField] DarkVignette darkVignette;
    [SerializeField] private float attackedVignetteIntensity = 0.35f;
    private Vignette vignette;
    public int playerHP = 5;
    public int maxPlayerHP = 5;
    private float _hitInvincibilityUntil;

    /// <summary>
    /// playerHP/maxPlayerHP 값이 변경되었음을 UI 구독자에게 알리는 이벤트.
    /// </summary>
    public event Action OnHpChanged;

    [Header("플레이어 무적 상태 표시 관련")]
    public bool isUnBeatHit = false;
    public bool isUnBeatDash = false;
    [Tooltip("피격용 무적 코루틴")]
    public Coroutine unbeatRoutineHit;
    [Tooltip("대쉬용 무적 코루틴")]
    public Coroutine unbeatRoutineDash;
    private float _extraDashUnbeatTime = 0.0f;          //중강에서 사용할 돌진 종료 후 추가 무적 시간
    private const float _dashUnbeatCoyoteTime = 0.15f;         //조작감 향상을 위한 돌진 무적 이펙트 종료 후 추가 무적 시간
    [SerializeField] MeshRenderer playerMeshRenderer;
    [Tooltip("플레이어가 무적 상태일 때 적용할 머티리얼 (0: 기본, 1: 피격 무적)")]
    public List<Material> playerMaterials = new List<Material>();
    [Tooltip("대쉬 무적 연출용 쉴드")]
    public GameObject dashShield;

    // 증강 스탯 재계산에 사용할 maxPlayerHP의 기준값
    private int _baseMaxPlayerHP;

    void Awake()
    {
        _baseMaxPlayerHP = maxPlayerHP;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dashShield.SetActive(false);
        rb = GetComponent<Rigidbody>();
        // Start는 씬의 모든 Awake 이후 실행되므로 여기서 구독하면 PlayerStats.Awake 순서와 무관하다.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += ApplyAugmentStats;
        }

        ApplyAugmentStats();

        volume = FindFirstObjectByType<Volume>();

        if (volume.profile.TryGet<Vignette>(out var tmpVignette))
        {
            vignette = tmpVignette;
        }
        attackedVignetteIntensity = 0.35f;
        SetVignetteIntensity(0.0f);
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= ApplyAugmentStats;
        }
    }

    /// <summary>
    /// 플레이어 체력 정보를 필요로 하는 애들 모두 호출
    /// </summary>
    void UpdateHpInfoToOthers()
    {
        //체력에 따라 시야 vignette 어둡기 처리
        darkVignette.UpdateVignetteDarkness(playerHP, maxPlayerHP);

        OnHpChanged?.Invoke();
    }

    #region 피격 처리 및 회복

    /// <summary>
    /// PlayerStats의 MaxHp 증강을 기준값에 적용해 maxPlayerHP를 재계산한다.
    /// 최대치가 늘면 늘어난 만큼 playerHP를 올리고, 줄면 playerHP를 최대치로 clamp한다.
    /// 변경된 값은 OnHpChanged 구독자와 Dark Vignette에 알린다.
    /// </summary>
    private void ApplyAugmentStats()
    {
        if (PlayerStats.Instance == null)
        {
            return;
        }

        int newMax = Mathf.Max(1, PlayerStats.Instance.ApplyInt(StatType.MaxHp, _baseMaxPlayerHP));

        if (newMax > maxPlayerHP)
        {
            playerHP += newMax - maxPlayerHP;
        }
        maxPlayerHP = newMax;
        playerHP = Mathf.Min(playerHP, maxPlayerHP);
        UpdateHpInfoToOthers();
    }

    /// <summary>
    /// 모든 전투 피해가 거치는 단일 피해 진입점이다. DamageInfo의 Kind별 무적 정책을 검사한 뒤 ReduceHealth로 체력을 감소시킨다.
    /// damageInfo는 피해량과 피해 유형을 제공하며, 피해가 적용되었으면 true를 반환한다.
    /// EnemyContact는 피격 무적·대쉬 무적 중에는 거부되고, 적용되면 2초 피격 무적을 시작한다.
    /// NoRushReflection은 피격·돌진 무적을 우회해 적용하지만, 새로운 피격 무적을 부여하지 않는다.
    /// 그 외 Kind는 무적 정책 우회를 막기 위해 거부하며, 낙하 피해는 ApplyFallDamage 경로에서 처리한다.
    /// </summary>
    public bool TakeDamage(DamageInfo damageInfo)
    {
        if (!damageInfo.HasSource || damageInfo.IsLethal || damageInfo.Amount <= 0)
        {
            return false;
        }

        switch (damageInfo.Kind)
        {
            case DamageKind.EnemyContact:
                // 전투 피해는 피격 무적과 대쉬 무적 중에는 적용되지 않는다.
                if (isUnBeatHit || isUnBeatDash)
                {
                    return false;
                }
                break;
            case DamageKind.NoRushReflection:
                break;
            default:
                // 플레이어가 수신하지 않는 Kind는 무적 정책 우회를 막기 위해 거부한다.
                return false;
        }

        ReduceHealth(damageInfo.Amount);

        if (damageInfo.Kind == DamageKind.EnemyContact)
        {
            // 접촉 피해는 실제로 적용되었을 때만 2초 피격 무적을 시작한다.
            ApplyHitInvincibility(HIT_INVINCIBILITY_SECONDS);
        }

        return true;
    }

    /// <summary>
    /// hp만큼 플레이어 체력을 감소시키고 피격 표시, 체력 변경 통지와 사망 판정을 수행한다.
    /// hp는 감소량이며 playerHP를 갱신하고 0 이하가 되면 게임오버를 요청한다.
    /// </summary>
    private void ReduceHealth(int hp)
    {
        //플레이어 체력 감소 처리
        playerHP -= hp;
        //피격 vignette 처리
        SetVignetteIntensity(attackedVignetteIntensity);

        UpdateHpInfoToOthers();
        // 사망 처리
        if (playerHP <= 0)
        {
            gameManager.PlayerDie();
        }
    }

    /// <summary>
    /// URP Vignette의 강도 값을 적용한다.
    /// intensity는 적용할 강도이며 Vignette override 상태와 값을 변경한다.
    /// </summary>
    private void SetVignetteIntensity(float intensity)
    {
        vignette.intensity.overrideState = true;
        vignette.intensity.value = intensity;
    }

    /// <summary>
    /// healingInfo에 따라 플레이어 HP를 회복하는 IHealable 진입점이다.
    /// 회복량이 0 이하이거나 비활성 상태이거나 사망했거나 이미 최대 체력이면 상태를 변경하지 않고 false를 반환한다.
    /// 실제로 playerHP가 증가하면 maxPlayerHP를 초과하지 않도록 clamp하고 OnHpChanged 구독자에게 알린 뒤 true를 반환한다.
    /// </summary>
    public bool ReceiveHealing(HealingInfo healingInfo)
    {
        if (healingInfo.Amount <= 0
            || !healingInfo.HasSource
            || !isActiveAndEnabled
            || playerHP <= 0
            || playerHP >= maxPlayerHP)
        {
            return false;
        }

        playerHP = Mathf.Min(playerHP + healingInfo.Amount, maxPlayerHP);
        UpdateHpInfoToOthers();
        return true;
    }

    #endregion


    #region 무적 처리 관련
    /// <summary>
    /// time초의 피격 무적을 부여한다.
    /// 이미 무적이면 현재 종료 시각과 새 종료 시각 중 더 늦은 시각을 유지한다.
    /// </summary>
    public void ApplyHitInvincibility(float time)
    {
        if (time <= 0f)
            return;

        float requestedUntil = Time.time + time;

        if (isUnBeatHit)
        {
            _hitInvincibilityUntil = Mathf.Max(_hitInvincibilityUntil, requestedUntil);
            return;
        }

        _hitInvincibilityUntil = requestedUntil;
        isUnBeatHit = true;
        unbeatRoutineHit = StartCoroutine(UnBeatTimeForHit());
    }

    /// <summary>
    /// 대쉬 무적 활성화 함수 (외부)
    /// 기본적으로 대쉬 종료 후 이펙트 꺼진 상태로 0.2초 추가 무적 적용
    /// </summary>
    /// <param name="time">지속 시간(초)</param>
    public void ApplyDashInvincibility(float time)
    {
        isUnBeatDash = true;
        if (unbeatRoutineDash != null) StopCoroutine(unbeatRoutineDash);
        unbeatRoutineDash = StartCoroutine(UnBeatTimeForDash(time));
    }

    /// <summary>
    /// _hitInvincibilityUntil까지 피격 무적과 표시 효과를 유지한다.
    /// 종료되면 표시 효과, isUnBeatHit, unbeatRoutineHit를 초기화한다.
    /// </summary>
    private IEnumerator UnBeatTimeForHit()
    {
        playerMeshRenderer.material = playerMaterials[1];

        while (Time.time < _hitInvincibilityUntil)
            yield return null;

        SetVignetteIntensity(0f);
        playerMeshRenderer.material = playerMaterials[0];
        isUnBeatHit = false;
        unbeatRoutineHit = null;
    }

    /// <summary>
    /// 대쉬 무적 처리 코루틴
    /// </summary>
    /// <param name="sec">무적 시간(초)</param>
    /// <returns></returns>
    private IEnumerator UnBeatTimeForDash(float sec)
    {
        //무적 상태 이펙트 보여주기 (쉴드)
        dashShield.SetActive(true);
        //돌진 동안 무적
        yield return new WaitForSeconds(sec);

        //돌진 종료 후 추가 무적
        dashShield.SetActive(false);
        yield return new WaitForSeconds(_extraDashUnbeatTime);

        //추가 무적 (코요테 타임)
        yield return new WaitForSeconds(_dashUnbeatCoyoteTime);

        //무적 해제
        isUnBeatDash = false;
    }

    /// <summary>
    /// 기본 무적 여부와 관계없이 damage만큼 낙하 피해를 적용한다. 
    /// 생존하면 invicibilitySeconds동안 피격 무적을 부여하고 true를 반환하며,
    /// 이미 사망했거나 피해로 사망하면 false를 반환한다. 
    /// </summary>
    public bool ApplyFallDamage(int damage, float invincibilitySeconds)
    {
        if (playerHP <= 0)
        {
            return false;
        }

        // 낙하 피해는 전투 피해와 별도 경로로 무적과 관계없이 적용한다.
        ReduceHealth(damage);

        if (playerHP <= 0)
        {
            return false;
        }

        ApplyHitInvincibility(invincibilitySeconds);
        return true;
    }

    #endregion
}
