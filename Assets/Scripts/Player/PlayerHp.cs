using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class PlayerHp : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] GameManager gameManager;
    [SerializeField] private Volume volume;
    [SerializeField] DarkVignette darkVignette;
    [SerializeField] private float attackedVignetteIntensity = 0.35f;
    private Vignette vignette;
    public int playerHP = 5;
    public int maxPlayerHP = 5;

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
    private float _dashUnbeatCoyoteTime = 0.2f;         //조작감 향상을 위한 돌진 무적 이펙트 종료 후 추가 무적 시간
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

    public void PlayerAttacked(int damage)
    {
        //플레이어 체력 감소 처리
        playerHP -= damage;
        //피격 vignette 처리
        SetVignetteIntensity(attackedVignetteIntensity);

        UpdateHpInfoToOthers();
        // 사망 처리
        if (playerHP <= 0)
        {
            gameManager.PlayerDie();
        }
    }

    private void SetVignetteIntensity(float intensity)
    {
        vignette.intensity.overrideState = true;
        vignette.intensity.value = intensity;
    }

    /// <summary>
    /// amount만큼 플레이어 HP를 회복한다. playerHP는 maxPlayerHP를 초과하지 않는다.
    /// 변경된 playerHP를 OnHpChanged 구독자에게 알려 UI에 반영한다.
    /// </summary>
    public void Heal(int amount)
    {
        playerHP = Mathf.Min(playerHP + amount, maxPlayerHP);
        UpdateHpInfoToOthers();
    }

    #endregion


    #region 무적 처리 관련
    /// <summary>
    /// 피격 무적 활성화 함수 (내부)
    /// </summary>
    /// <param name="time">지속 시간(초)</param>
    void ApplyHitInvincibility(float time)
    {
        isUnBeatHit = true;
        if (unbeatRoutineHit != null) StopCoroutine(unbeatRoutineHit);
        unbeatRoutineHit = StartCoroutine(UnBeatTimeForHit(time));
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
    /// 피격 무적 처리 코루틴
    /// </summary>
    /// <param name="sec">무적 시간(초)</param>
    /// <returns></returns>
    private IEnumerator UnBeatTimeForHit(float sec)
    {
        //무적 상태 이펙트 보여주기(블링크)
        playerMeshRenderer.material = playerMaterials[1];

        //무적 상태 유지
        yield return new WaitForSeconds(sec);

        //피격 피드벡 모두 끄기
        SetVignetteIntensity(0.0f);
        playerMeshRenderer.material = playerMaterials[0];

        //무적 해제
        isUnBeatHit = false;
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


    #endregion

    #region 콜라이더 처리 관련

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("NoAbsortEnemy"))
        {
            //Debug.Log("Player Attacked");
            if (!isUnBeatHit && !isUnBeatDash)
            {
                PlayerAttacked(1);
                ApplyHitInvincibility(2f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HealPack") && playerHP < maxPlayerHP)
        {
            Heal(1);
            Destroy(other.gameObject);
        }
    }

    #endregion
}