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
    IEnumerator UnBeatTime()
    {
        isUnBeatNr = true;
        playerHp.PlayerAttacked(1);
        yield return new WaitForSeconds(1);
        isUnBeatNr = false;
    }


    void TakeDamageNr(int damage)
    {
        healthNr -= damage;
        StartCoroutine(PlayHitEffect());
        CheckHealthNr();
    }
    IEnumerator PlayHitEffect()
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
    Color ChangeColor(string color)
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


    IEnumerator DieNr()
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
    protected void CheckHealthNr()
    {
        // 부모 isDead까지 함께 봐서 Kill() 경유와 무관하게 연쇄 처치/중복 이벤트를 막는다.
        if (healthNr <= 0 && !isDeadNr && !isDead)
        {
            isDeadNr = true;
            isDead = true;
            AugmentEvents.RaiseEnemyKilled(this);
            StartCoroutine(DieNr());
        }
    }

    //OnCollisionEnter에서 적이 죽은 적과 충돌했는지 검사하여 연쇄 충돌 효과 만듦.
    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && collision.gameObject.GetComponent<Enemy>().isDead)
        {
            TakeDamageNr(ApplyChainDamage(1));
        }
    }
}
