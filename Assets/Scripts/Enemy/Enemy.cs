using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static EnemyPool;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int health = 5;
    public float speed = 5f;
    public int knockbackForce = 10;
    public bool isDead = false;
    public PoolType poolType;
    public BoxCollider coll;
    public int enemyScore = 0;

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
    // 적 기본 설정!
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
    //적이 데미지를 입는 함수.
    protected void TakeDamage(int damage)
    {
        health -= damage;
        CheckHealth();
    }
    //적의 체력을 체크하는 함수. 체력이 0 이하이면 Die 코루틴을 호출함. 
    //단 Die 코루틴은 넉백 사망 등을 고려해 만들어졌기 때문에 사망형태에 따라 변경할 필요가 있을 수 있음. 
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
    /// 체력 검사에서 처치가 확정된 적의 처치 이벤트와 점수를 한 번 등록하고 사망 처리를 시작한다.
    /// isDead가 이미 true면 아무 상태도 바꾸지 않으며, 처치 집계 후 Die(true) 코루틴을 실행한다.
    /// </summary>
    public void Kill()
    {
        if (isDead) return;
        isDead = true;
        AugmentEvents.RaiseEnemyKilled(this);
        GameManager.Instance.RecordEnemyKill(this);
        GameManager.Instance.AddScore(enemyScore);
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
        GameManager.Instance.RecordEnemyAbsorb(this);
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

    //사망 시 나오는 파편 효과를 생성하는 함수
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
            TakeDamage(ApplyChainDamage(5));
        }
        else if (other.CompareTag("Enemy") && other.gameObject.GetComponent<Enemy>().isDead)
        {
            if (gameObject.CompareTag("Enemy"))
            {
                enemyScore = other.gameObject.GetComponent<Enemy>().enemyScore + 1;
                TakeDamage(ApplyChainDamage(5));
            }
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
