using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static EnemyPool;

/// <summary>
/// 일반
/// </summary>
public class Enemy : MonoBehaviour, IDamageable, IDamageSource, IHealable
{
    [Header("Enemy Stats")]
    private int _maxHealth;
    public int health = 5;
    public float speed = 5f;
    public int knockbackForce = 10;
    public bool isDead = false;
    public PoolType poolType;
    public BoxCollider coll;
    public int enemyScore = 0;
    [Min(2)] public int enemyAttackDamage;

    [Header("References")]
    public PlayerController player;
    protected Rigidbody enemyRb;
    protected EnemyPool enemyPool;
    // 프리팹에 설정된 리지드바디 제약. 풀 재사용 시 복원한다.
    protected RigidbodyConstraints _defaultConstraints;

    [Header("Death Effects")]
    public float deathEffectForce = 5f;
    public int effectCount = 10;
    public TrailRenderer trail;

    [Header("Prefabs")]
    public GameObject deathEffectPrefab;
    public GameObject bulletPrefab;

    [Header("NavMesh")]
    public NavMeshAgent navMeshAgent;

    [Header("Material")]
    private Renderer rend;
    public Material liveMaterial;
    public Material deathMaterial;

    [Header("Absorb Aura")]
    // 흡수 대상 표시용 오러 셸(프리팹에 미리 배치된 자식, OnAbsorbTarget으로 켜고 끔)
    [SerializeField] private GameObject absorbAura;

    protected virtual void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = FindAnyObjectByType<PlayerController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed;
        trail = GetComponentInChildren<TrailRenderer>();
        coll = GetComponent<BoxCollider>();
        rend = GetComponent<Renderer>();

        // 사망 처리 중 변경된 제약을 복원하기 위해 프리팹 기본값을 먼저 보관한다.
        _defaultConstraints = enemyRb.constraints;
    }
    /// <summary>
    /// 풀 타입에 따른 적 체력과 이동 속도를 설정하고 풀 재사용 상태를 초기화한다.
    /// poolType과 pool을 사용하며, 현재 체력 기준선과 물리·흡수 상태를 갱신한다.
    /// </summary>
    public virtual void Initialize(PoolType poolType, EnemyPool pool)
    {
        // 풀 재사용 시 이전 흡수 오러가 남지 않도록 먼저 해제
        OnAbsorbTarget(false);
        ChangeMaterial(true);
        enemyScore = 0;
        this.poolType = poolType;
        enemyPool = pool;
        isDead = false;
        switch (poolType)
        {
            case PoolType.Basic:
                speed = 5f;
                health = 5;
                break;
            case PoolType.Boom:
                speed = 3f;
                health = 10;
                break;
            case PoolType.NoRush:
                speed = 2f;
                health = 20;
                break;
            case PoolType.NoAbsort:
                speed = 1f;
                health = 5;
                break;
        }

        // 회복이 스폰 체력 기준선을 초과하지 않도록 현재 풀 타입의 최대치를 함께 기록한다.
        _maxHealth = health;

        enemyRb.linearVelocity = Vector3.zero;
        enemyRb.angularVelocity = Vector3.zero;
        // 사망 처리 중 바뀐 리지드바디 제약을 프리팹 기본값으로 복원
        enemyRb.constraints = _defaultConstraints;
        navMeshAgent.speed = speed;
        coll.isTrigger = false;
        trail.enabled = false;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        navMeshAgent.speed = speed;
        // MoveTowardsPlayer();
        if (!isDead)
        {
            navMeshAgent.SetDestination(player.transform.position);
        }
    }
    //플레이어에게 이동하는 코드. Update에서 호출됨. NavMesh로 변경될 수 있음. 
    protected void MoveTowardsPlayer()
    {
        if (isDead) return;
        Vector3 moveDirection = player.transform.position - transform.position;
        enemyRb.MovePosition(transform.position + moveDirection.normalized * speed * Time.deltaTime);
        enemyRb.linearVelocity = moveDirection.normalized * speed;
    }
    /// <summary>
    /// 피해 정보를 받아 health를 감소시키고 CheckHealth로 사망 여부를 확인하는 IDamageable 진입점이다.
    /// damageInfo의 Amount를 사용하고 IsLethal이면 체력을 즉사 임계로 보내며, 처리 여부를 bool로 반환한다.
    /// </summary>
    public virtual bool TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || !damageInfo.HasSource)
        {
            return false;
        }

        // 즉사 요청은 피해량과 무관하게 사망 임계로 진입시킨다.
        if (damageInfo.IsLethal)
        {
            health = 0;
        }
        else
        {
            health -= damageInfo.Amount;
        }

        CheckHealth();
        return true;
    }

    /// <summary>
    /// 적 수신자가 처리할 수 있는 회복 정보인지 검사한다.
    /// healingInfo의 회복량과 HasSource로 회복원 참조 존재를 확인해 유효하면 true를 반환한다.
    /// </summary>
    protected bool IsValidHealingInfo(HealingInfo healingInfo)
    {
        return healingInfo.Amount > 0
            && healingInfo.HasSource;
    }

    /// <summary>
    /// healingInfo에 따라 health를 회복하는 IHealable 진입점이다.
    /// 회복량이 0 이하이거나 사망했거나 비활성 상태이면 상태를 변경하지 않고 false를 반환한다.
    /// 실제로 health가 증가하면 _maxHealth를 초과하지 않도록 clamp해 true를 반환한다.
    /// </summary>
    public virtual bool ReceiveHealing(HealingInfo healingInfo)
    {
        if (!IsValidHealingInfo(healingInfo) || isDead || !gameObject.activeInHierarchy || health >= _maxHealth)
        {
            return false;
        }

        health = Mathf.Min(health + healingInfo.Amount, _maxHealth);
        return true;
    }

    /// <summary>
    /// 현재 health를 확인해 0 이하이면 내부 처치 경로를 시작한다.
    /// health를 읽고, 처치 조건을 만족하면 Kill을 호출한다.
    /// </summary>
    protected void CheckHealth()
    {
        if (health <= 0)
        {
            Kill();
        }
    }
    /// <summary>
    /// 외부에서 사망 처리를 요청할 때 사용. 코루틴을 적 자신이 실행하므로 호출자가 비활성 상태여도 동작함.
    /// isDead가 true면 중복 실행하지 않는다. 점수 처리는 하지 않는다.
    /// </summary>
    /// <param name="isKnockback">넉백 사망 여부</param>
    public void DoDie(bool isKnockback)
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(Die(isKnockback));
    }
    /// <summary>
    /// 체력 검사에서 처치가 확정된 적의 처치 이벤트와 킬 통계·점수를 한 번 등록하고 사망 처리를 시작한다.
    /// isDead가 이미 true면 아무 상태도 바꾸지 않으며, EnemyKilled 이벤트 후 킬 통계를 기록하고 AddKillScore로 점수를 반영한 뒤 Die(true) 코루틴을 실행한다.
    /// </summary>
    protected void Kill()
    {
        if (isDead) return;
        isDead = true;
        AugmentEvents.RaiseEnemyKilled(this);
        GameManager.Instance.RecordEnemyOutcome(StatisticsManager.GameStatisticType.EnemyKill, poolType);
        GameManager.Instance.AddKillScore(this);
        StartCoroutine(Die(true));
    }

    protected virtual void OnDisable()
    {
        // 풀 반환 시 오러 셸이 남지 않도록 해제
        OnAbsorbTarget(false);
    }

    /// <summary>
    /// 흡수 대상으로 지정/해제될 때 프리팹에 미리 배치된 노란 오러 셸을 켜고 끈다.
    /// </summary>
    /// <param name="isTarget">true면 표시, false면 숨김</param>
    public void OnAbsorbTarget(bool isTarget)
    {
        if (absorbAura != null && absorbAura.activeSelf != isTarget)
            absorbAura.SetActive(isTarget);
    }

    /// <summary>
    /// 적 흡수를 시도한다. 사망·비활성·흡수 불가(NoAbsort)·참조 누락 가드를 통과해야 성공하며, 성공 시 isDead를 true로 바꾸고 흡수 이펙트와 풀 반환을 처리한다.
    /// 성공 시 true, 가드에서 실패하면 false를 반환한다.
    /// </summary>
    public bool TryAbsorb(EnemyAbsorbEffect lightBallPrefab, Transform playerTarget, UnityEvent rewardOnArrival, Transform uiWorldMarker)
    {
        if (isDead || !gameObject.activeInHierarchy || poolType == PoolType.NoAbsort)
            return false;

        if (playerTarget == null || enemyPool == null)
        {
            return false;
        }

        isDead = true;
        // 가드를 모두 통과한 성공 경로에서만 집계한다. isDead가 먼저 세워지므로 재호출 시 중복 집계되지 않는다.
        GameManager.Instance.RecordEnemyOutcome(StatisticsManager.GameStatisticType.EnemyAbsorb, poolType);
        Vector3 effectPosition = absorbAura != null ? absorbAura.transform.position : transform.position;

        // 풀에 속한 자식 오러와 별개로 잠깐 남을 이펙트 (원본이 비활성일 수 있어 Get에서 명시 활성화)
        if (absorbAura != null)
        {
            GameObject auraEffect = EffectPool.Get("AbsorbAuraFlash_" + poolType, () => Instantiate(absorbAura), effectPosition, absorbAura.transform.rotation);

            auraEffect.transform.localScale = absorbAura.transform.lossyScale;
            auraEffect.SetActive(true);
            EffectPool.ReleaseAfter(auraEffect, 0.35f);
        }

        if (lightBallPrefab != null)
        {
            EnemyAbsorbEffect lightBall = EffectPool.Get(lightBallPrefab.gameObject, effectPosition, Quaternion.identity).GetComponent<EnemyAbsorbEffect>();

            lightBall.Initialize(playerTarget, uiWorldMarker, rewardOnArrival);
        }

        // 흡수 사망 시 즉시 처리해야 하는 subclass(자폭 등)를 위한 훅. 풀 반환 전에 호출한다.
        AugmentEvents.RaiseEnemyAbsorbed(this);
        OnAbsorbed();

        enemyPool.DieEnemy(gameObject, poolType);
        return true;
    }

    /// <summary>
    /// 흡수 사망 시 TryAbsorb이 풀 반환 전에 호출하는 훅. 기본 동작은 없으며 자폭 등 즉시 처리가 필요한 subclass에서 override한다.
    /// </summary>
    protected virtual void OnAbsorbed()
    {
    }

    /// <summary>
    /// 사망 연출과 물리 상태를 적용한 뒤 적을 풀에 반환한다.
    /// isKnockback이 true면 사망 이펙트와 넉백을 추가하고 반환 전 대기 시간을 사용한다.
    /// </summary>
    public virtual IEnumerator Die(bool isKnockback)
    {
        ChangeMaterial(false);
        if (isKnockback)
        {

            trail.enabled = true;
            // navMeshAgent.enabled = false;
            coll.isTrigger = true;
            enemyRb.constraints = RigidbodyConstraints.FreezePositionY;

            PlayDeathEffect();  //사망 시 나오는 파편 효과를 생성하는 함수. 파편 모양은 Enemy보다 작은 회색 큐브.
            Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
            // 풀 재사용 시 누적되지 않도록 필드 대신 사용 시점에 증강을 적용해 계산한다.
            enemyRb.AddForce(knockbackDirection * GetAugmentedKnockbackForce(), ForceMode.Impulse);

            yield return new WaitForSeconds(0.5f);
            trail.enabled = false;
            // navMeshAgent.enabled = true;
            coll.isTrigger = false;
            // X·Z 회전을 한 번에 고정 (개별로 대입하면 뒤의 값으로 덮어써짐)
            enemyRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            yield return new WaitForSeconds(0.1f);

        }
        else
        {
            // 이곳에 흡수 공격 사망 이펙트 추가 가능
        }
        enemyPool.DieEnemy(gameObject, poolType);
    }

    /// <summary>
    /// 사망 위치에 설정된 수만큼 파편 Rigidbody 이펙트를 생성하고 무작위 방향으로 튕긴다.
    /// effectCount, deathEffectPrefab, deathEffectForce를 사용해 파편 오브젝트를 활성화한다.
    /// </summary>
    public void PlayDeathEffect()
    {
        for (int i = 0; i < effectCount; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            GameObject effect = EffectPool.Get(deathEffectPrefab, transform.position, Quaternion.identity);
            // 풀 재사용 시 이전 관성이 남지 않도록 속도를 완전히 되돌린 뒤 튕겨낸다.
            Rigidbody effectRb = effect.GetComponent<Rigidbody>();
            effectRb.linearVelocity = Vector3.zero;
            effectRb.angularVelocity = Vector3.zero;
            effectRb.AddForce(randomOffset.normalized * Random.Range(1f, deathEffectForce), ForceMode.Impulse);
        }
        // PlayDeathParticle();
    }

    //OnTriggerEnter에서 드롭킥을 맞았는지 검사. 
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DropkickRange"))
        {
            enemyScore += 1;
            // 드롭킥 피해는 피해원인 드롭킥 컴포넌트가 메서드로 전달하도록 위임한다.
            PlayerAttack_Dropkick kick = other.GetComponentInParent<PlayerAttack_Dropkick>();
            if (kick != null)
            {
                kick.ApplyDropkickDamage(this);
            }
        }
        else if (other.CompareTag("Enemy") && other.gameObject.GetComponent<Enemy>().isDead)
        {
            if (gameObject.CompareTag("Enemy"))
            {
                Enemy corpse = other.gameObject.GetComponent<Enemy>();
                enemyScore = corpse.enemyScore + 1;
                corpse.DealChainDamageTo(this, 5);
            }
        }
    }

    /// <summary>
    /// 사망한 적이 대상에게 연쇄 피해를 전달한다.
    /// target은 피해를 받을 객체, baseDamage는 ChainDamage 증강 적용 전 피해량이며 적용 여부를 반환한다.
    /// </summary>
    public bool DealChainDamageTo(IDamageable target, int baseDamage)
    {
        if (!isDead || target == null)
        {
            return false;
        }

        int damage = ApplyChainDamage(baseDamage);
        return target.TakeDamage(new DamageInfo(damage, DamageKind.EnemyChain, this));
    }

    /// <summary>
    /// 접촉 중인 상대의 콜라이더 상위 체인에서 PlayerHp를 찾아 EnemyContact 피해 enemyAttackDamage을 시도한다.
    /// 무적 판정과 접촉 무적 부여는 수신자인 PlayerHp.TakeDamage가 담당한다.
    /// </summary>
    protected virtual void OnCollisionStay(Collision collision)
    {
        PlayerHp hitPlayer = collision.collider.GetComponentInParent<PlayerHp>();
        if (hitPlayer != null)
        {
            IDamageable damageTarget = hitPlayer;
            damageTarget.TakeDamage(new DamageInfo(enemyAttackDamage, DamageKind.EnemyContact, this));
        }
    }

    /// <summary>
    /// PlayerStats의 KnockbackForce 증강을 적용한 넉백 강도를 반환한다.
    /// 필드 knockbackForce는 변경하지 않으며, PlayerStats.Instance가 없으면 필드 값을 그대로 반환한다.
    /// </summary>
    protected float GetAugmentedKnockbackForce()
    {
        if (PlayerStats.Instance == null)
        {
            return knockbackForce;
        }

        return PlayerStats.Instance.Apply(StatType.KnockbackForce, knockbackForce);
    }

    /// <summary>
    /// baseDamage에 PlayerStats의 ChainDamage 증강을 적용한 정수 피해를 반환한다.
    /// PlayerStats.Instance가 없으면 baseDamage를 그대로 반환한다.
    /// </summary>
    protected int ApplyChainDamage(int baseDamage)
    {
        if (PlayerStats.Instance == null)
        {
            return baseDamage;
        }

        return PlayerStats.Instance.ApplyInt(StatType.ChainDamage, baseDamage);
    }

    /// <summary>
    /// 적의 생존 여부에 맞춰 기본 Renderer 머티리얼을 변경한다.
    /// isLive가 true면 liveMaterial, false면 deathMaterial을 적용한다.
    /// </summary>
    public virtual void ChangeMaterial(bool isLive)
    {
        // NoRush는 루트에 Renderer가 없을 수 있어 rend가 null일 수 있다.
        if (poolType == PoolType.NoRush || rend == null) return;
        if (isLive)
        {
            rend.material = liveMaterial;
        }
        else
        {
            rend.material = deathMaterial;
        }
    }
}
