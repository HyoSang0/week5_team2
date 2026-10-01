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
    }
    // 적 기본 설정!
    public void Initialize(PoolType poolType, EnemyPool pool)
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
        if (health <= 0 && !isDead)
        {
            isDead = true;
            GameManager.Instance.AddScore(enemyScore);
            StartCoroutine(Die(true));
        }
    }
    // 외부에서 사망 처리를 요청할 때 사용. 코루틴을 적 자신이 실행하므로 호출자가 비활성 상태여도 동작함.
    public void DoDie(bool isKnockback)
    {
        StartCoroutine(Die(isKnockback));
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

    public bool TryAbsorb(EnemyAbsorbEffect lightBallPrefab, Transform playerTarget, UnityEvent rewardOnArrival)
    {
        if (isDead || !gameObject.activeInHierarchy || poolType == PoolType.NoAbsort)
            return false;

        if (lightBallPrefab == null || playerTarget == null || enemyPool == null)
        {          
            return false;
        }

        isDead = true;
        Vector3 effectPosition = absorbAura != null ? absorbAura.transform.position : transform.position;

        // 풀에 속한 자식 오러와 별개로 잠깐 남을 이펙트
        if (absorbAura != null)
        {
            GameObject auraEffect = Instantiate(absorbAura, effectPosition, absorbAura.transform.rotation);

            auraEffect.transform.localScale = absorbAura.transform.lossyScale;
            auraEffect.SetActive(true);
            Destroy(auraEffect, 0.35f);
        }

        EnemyAbsorbEffect lightBall = Instantiate(lightBallPrefab, effectPosition, Quaternion.identity);

        lightBall.Initialize(playerTarget, rewardOnArrival);

        enemyPool.DieEnemy(gameObject, poolType);
        return true;
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
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);

            yield return new WaitForSeconds(0.5f);
            trail.enabled = false;
            // navMeshAgent.enabled = true;
            coll.isTrigger = false;
            enemyRb.constraints = RigidbodyConstraints.FreezeRotationX;
            enemyRb.constraints = RigidbodyConstraints.FreezeRotationZ;
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
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            effect.GetComponent<Rigidbody>().AddForce(randomOffset.normalized * Random.Range(1f, deathEffectForce), ForceMode.Impulse);
        }
        // PlayDeathParticle();
    }

    //OnTriggerEnter에서 드롭킥을 맞았는지 검사. 
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DropkickRange"))
        {
            enemyScore += 1;
            TakeDamage(5);
        }
        else if (other.CompareTag("Enemy") && other.gameObject.GetComponent<Enemy>().isDead)
        {
            if (gameObject.CompareTag("Enemy"))
            {
                enemyScore = other.gameObject.GetComponent<Enemy>().enemyScore + 1;
                TakeDamage(5);
            }
        }
    }

    public virtual void ChangeMaterial(bool isLive)
    {
        if (poolType == PoolType.NoRush) return;
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
