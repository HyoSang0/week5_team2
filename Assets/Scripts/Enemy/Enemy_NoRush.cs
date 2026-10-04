using System.Collections;
using UnityEngine;

public class Enemy_NoRush : Enemy
{
    [Header("Enemy Stats")]
    public int healthNr = 20;

    bool isDeadNr = false;
    bool isUnBeatNr = false;

    [Header("References")]
    public PlayerHp playerHp;

    // 풀 재사용 시 복원하기 위해 Awake에서 캐시하는 프리팹 초기 체력
    private int _defaultHealthNr;

    Material[] mat;

    protected override void Awake()
    {
        // 부모 Awake에서 enemyRb/coll/trail/rend/navMeshAgent를 초기화하므로 반드시 먼저 호출한다.
        base.Awake();

        // 프리팹 인스펙터에 설정된 체력을 기억해 재사용 시 복원한다.
        _defaultHealthNr = healthNr;

        playerHp = player.gameObject.GetComponent<PlayerHp>();
        mat = new Material[5];

        for (int i = 0; i < 5; i++)
        {
            mat[i] = transform.GetChild(i).GetComponent<MeshRenderer>().material;
        }

    }
    /// <summary>
    /// 풀 재사용 시 base.Initialize을 호출한 뒤 NoRush 전용 상태(체력/사망/무적 플래그, 히트 색)를
    /// 프리팹 기본값으로 복원한다.
    /// </summary>
    /// <param name="poolType">소환 풀 타입</param>
    /// <param name="pool">소유 풀</param>
    public override void Initialize(EnemyPool.PoolType poolType, EnemyPool pool)
    {
        base.Initialize(poolType, pool);

        // 재사용 시 무적/미초기화 상태로 스폰되지 않도록 NoRush 전용 상태를 되돌린다.
        healthNr = _defaultHealthNr;
        isDeadNr = false;
        isUnBeatNr = false;
        for (int i = 0; i < mat.Length; i++)
        {
            // mat[i].color = Color.red;
            mat[i] = liveMaterial;
        }
    }
    //OnTriggerEnter에서 드롭킥을 맞았는지 검사. 맞았으면 플레이어에게 반사 대미지.
    protected override void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Foot"))
        {
            if (!isUnBeatNr)
            {
                StartCoroutine(UnBeatTime());
            }
        }
    }

    /// <summary>
    /// 플레이어에게 반사 피해 1을 시도하고 1초간 반사 쿨다운을 유지한다.
    /// 피해는 PlayerHp.TakeDamage(NoRushReflection)로 적용하며 무적 중에는 무시되고, 새로운 피격 무적은 부여하지 않는다.
    /// </summary>
    private IEnumerator UnBeatTime()
    {
        isUnBeatNr = true;
        IDamageable damageTarget = playerHp;
        damageTarget.TakeDamage(new DamageInfo(1, DamageKind.NoRushReflection, this));
        yield return new WaitForSeconds(1);
        isUnBeatNr = false;
    }


    /// <summary>
    /// 공용 피해 진입점을 NoRush 전용 경로로 우회한다.
    /// damageInfo의 Amount를 TakeDamageNr로 전달해 healthNr을 감소시키며, IsLethal이면 사망 임계로 보내고 base.TakeDamage는 호출하지 않는다.
    /// </summary>
    public override bool TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || isDeadNr || !damageInfo.HasSource)
        {
            return false;
        }

        // 즉사 요청은 남은 체력과 무관하게 healthNr을 사망 임계로 보낸다.
        TakeDamageNr(damageInfo.IsLethal ? healthNr : damageInfo.Amount);
        return true;
    }

    /// <summary>
    /// 공용 회복 진입점을 NoRush 전용 경로로 우회한다.
    /// healingInfo의 Amount를 사용해 healthNr을 회복하며, base.ReceiveHealing과 상속된 health는 사용하지 않는다.
    /// 회복량이 0 이하이거나 사망했거나 비활성 상태이면 상태를 변경하지 않고 false를 반환하며,
    /// 실제로 healthNr이 증가하면 프리팹 기준 체력을 초과하지 않도록 clamp해 true를 반환한다.
    /// </summary>
    public override bool ReceiveHealing(HealingInfo healingInfo)
    {
        if (!IsValidHealingInfo(healingInfo) || isDead || isDeadNr || !gameObject.activeInHierarchy || healthNr >= _defaultHealthNr)
        {
            return false;
        }

        healthNr = Mathf.Min(healthNr + healingInfo.Amount, _defaultHealthNr);
        return true;
    }

    /// <summary>
    /// NoRush 전용 healthNr을 감소시키고 피격 연출 및 사망 여부를 확인한다.
    /// damage는 감소량이며 healthNr과 피격 코루틴 상태를 변경한다.
    /// </summary>
    private void TakeDamageNr(int damage)
    {
        healthNr -= damage;
        StartCoroutine(PlayHitEffect());
        CheckHealthNr();
    }
    /// <summary>
    /// NoRush 피격 시 캐시된 머티리얼 배열의 값을 잠시 사망 머티리얼 참조로 바꾼다.
    /// mat 배열을 갱신하고 0.1초 뒤 생존 머티리얼 참조로 되돌린다.
    /// </summary>
    private IEnumerator PlayHitEffect()
    {
        for (int i = 0; i < mat.Length; i++)
        {
            mat[i] = deathMaterial;
        }
        yield return new WaitForSeconds(.1f);
        for (int i = 0; i < mat.Length; i++)
        {
            mat[i] = liveMaterial;

        }
    }
    /// <summary>
    /// 이름으로 지정된 색 문자열을 Unity Color 값으로 변환한다.
    /// color가 red 또는 gray이면 해당 색을, 그 외에는 white를 반환한다.
    /// </summary>
    private Color ChangeColor(string color)
    {
        if (color == "red")
        {
            return Color.red;
        }
        else if (color == "gray")
        {
            return Color.gray;
        }
        return Color.white;
    }


    /// <summary>
    /// NoRush 전용 사망 이펙트와 넉백을 재생한 뒤 적을 전용 풀 타입으로 반환한다.
    /// 넉백 힘과 사망 대기 시간을 사용하며 NoRush 물리 상태와 풀 소유권을 변경한다.
    /// </summary>
    private IEnumerator DieNr()
    {
        PlayDeathEffect();  //사망 시 나오는 파편 효과를 생성하는 함수. 파편 모양은 Enemy보다 작은 회색 큐브.
        Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
        // 풀 재사용 시 누적되지 않도록 필드 대신 사용 시점에 증강을 적용해 계산한다.
        enemyRb.AddForce(knockbackDirection * GetAugmentedKnockbackForce(), ForceMode.Impulse);
        // enemyRb.linearVelocity = knockbackDirection * knockbackForce;
        yield return new WaitForSeconds(0.5f);

        yield return new WaitForSeconds(0.1f);
        ChangeColor("red");
        enemyPool.DieEnemy(gameObject, EnemyPool.PoolType.NoRush);
    }
    /// <summary>
    /// healthNr이 0 이하이고 isDeadNr/isDead가 모두 false일 때 사망을 확정한다.
    /// EnemyKilled 이벤트 발생 후 일반 Enemy.Kill과 동일하게 RecordEnemyKill로 처치를 집계하고
    /// GameManager.Instance.AddScore(enemyScore)로 점수를 등록한 뒤 DieNr 코루틴을 시작한다. isDead도 함께 세워 Kill() 경유의 중복을 막는다.
    /// </summary>
    protected void CheckHealthNr()
    {
        // 부모 isDead까지 함께 봐서 Kill() 경유와 무관하게 연쇄 처치/중복 이벤트를 막는다.
        if (healthNr <= 0 && !isDeadNr && !isDead)
        {
            isDeadNr = true;
            isDead = true;
            AugmentEvents.RaiseEnemyKilled(this);
            // 일반 Enemy.Kill과 같은 순서로 처치 집계와 점수 등록을 수행해 ScoreMultiplier 소비 경로를 일관되게 유지한다.
            GameManager.Instance.RecordEnemyKill(this);
            GameManager.Instance.AddScore(enemyScore);
            StartCoroutine(DieNr());
        }
    }

    //OnCollisionEnter에서 적이 죽은 적과 충돌했는지 검사하여 연쇄 충돌 효과 만듦.
    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && collision.gameObject.GetComponent<Enemy>().isDead)
        {
            Enemy corpse = collision.gameObject.GetComponent<Enemy>();
            corpse.DealChainDamageTo(this, 1);
        }
    }
}
