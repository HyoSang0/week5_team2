using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static EnemyPool;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int health = 5;
    public int speed = 5;
    public int knockbackForce = 10;
    public bool isDead = false;
    public PoolType poolType;

    [Header("References")]
    public PlayerController player;
    protected Rigidbody enemyRb;
    protected EnemyPool enemyPool;

    [Header("Death Effects")]
    public float deathEffectForce = 5f;
    public int effectCount = 10;
    
    [Header("Prefabs")]
    public GameObject deathEffectPrefab;
    public GameObject deathParticle;
    public GameObject bulletPrefab;

    [Header("NavMesh")]
    public NavMeshAgent navMeshAgent;
    protected void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = FindAnyObjectByType<PlayerController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed;
   }

    // 적 기본 설정!
    public void Initialize(PoolType poolType, EnemyPool pool)
    {

        this.poolType = poolType;
        enemyPool = pool;
        switch (poolType)
        {
            case PoolType.Basic:
                speed = 5;
                health = 5;
                break;
            case PoolType.Boom:
                speed = 3;
                health = 10;
                break;
            case PoolType.NoRush:
                speed = 2;
                health = 20;
                break;
            case PoolType.NoAbsort:
                speed = 1;
                health = 5;
                break;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        
    }

    // Update is called once per frame
    protected void Update()
    {
        // MoveTowardsPlayer();
        navMeshAgent.SetDestination(player.transform.position);
    }
    //플레이어에게 이동하는 코드. Update에서 호출됨. NavMesh로 변경될 수 있음. 
    protected void MoveTowardsPlayer()
    {
        if(isDead) return;
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
        if(health <= 0 && !isDead)
        {
            isDead = true;
            StartCoroutine(Die(true));
        }
    }
    public IEnumerator Die(bool isKnockback)
    {
        if (isKnockback)
        {
            PlayDeathEffect();  //사망 시 나오는 파편 효과를 생성하는 함수. 파편 모양은 Enemy보다 작은 회색 큐브.
            Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            // enemyRb.linearVelocity = knockbackDirection * knockbackForce;
            yield return new WaitForSeconds(0.5f);
            PlayDeathParticle();    // 사망 시 나오는 모래먼지 같은 파티클 시스템 작동 함수. 
            yield return new WaitForSeconds(0.5f);
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
        PlayDeathParticle();
    }

    //사망 시 나오는 모래먼지 같은 파티클 시스템 작동 함수
    protected void PlayDeathParticle()
    {
        GameObject deathParticleEffect = Instantiate(deathParticle, transform.position, Quaternion.identity);
        Destroy(deathParticleEffect, 1f);
        // GameObject deathParticleEffect = ParticlePool.Instance.particlePool.Get();
        // deathParticleEffect.transform.position = transform.position;
        // deathParticleEffect.transform.rotation = Quaternion.identity;
        // ParticlePool.Instance.particlePool.Release(deathParticleEffect);
    }

    //OnTriggerEnter에서 드롭킥을 맞았는지 검사. 
    protected void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("DropkickRange"))
        {
            TakeDamage(5);
        }
    }

    //OnCollisionEnter에서 적이 죽은 적과 충돌했는지 검사하여 연쇄 충돌 효과 만듦.
    protected void OnCollisionEnter(Collision collision)
    {
        if((collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("NoAbsortEnemy"))
            && collision.gameObject.GetComponent<Enemy>().isDead)
        {
            TakeDamage(5);
        }
    }
}
