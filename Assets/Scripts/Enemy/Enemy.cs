using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int health = 5;
    public int speed = 5;
    public int knockbackForce = 10;
    bool isDead = false;

    [Header("References")]
    public PlayerController player;
    private Rigidbody enemyRb;

    [Header("Death Effects")]
    public float deathEffectForce = 5f;
    public int effectCount = 10;
    
    [Header("Prefabs")]
    public GameObject deathEffectPrefab;
    public GameObject deathParticle;
    public GameObject bulletPrefab;
    protected void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = FindAnyObjectByType<PlayerController>();
    }
    public void Initialize()
    {
        health = 5;
        speed = 5;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        
    }

    // Update is called once per frame
    protected void Update()
    {
        MoveTowardsPlayer();
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
            StartCoroutine(Die());
        }
    }
    protected IEnumerator Die()
    {
        PlayDeathEffect();  //사망 시 나오는 파편 효과를 생성하는 함수. 파편 모양은 Enemy보다 작은 회색 큐브.
        Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
        enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
        // enemyRb.linearVelocity = knockbackDirection * knockbackForce;
        yield return new WaitForSeconds(0.5f);
        PlayDeathParticle();    // 사망 시 나오는 모래먼지 같은 파티클 시스템 작동 함수. 
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);    // 오브젝트 풀링 사용 시 변경 필요.
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
        if(collision.gameObject.CompareTag("Enemy") && collision.gameObject.GetComponent<Enemy>().isDead)
        {
            TakeDamage(5);
        }
    }
}
