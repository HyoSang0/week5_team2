using UnityEngine;
using System.Collections;

public class EnemyTemp : MonoBehaviour
{
    public int health = 5;
    public int speed = 5;
    public int knockbackForce = 10;
    private PlayerController player;
    public GameObject deathEffectPrefab;
    public float deathEffectForce = 5f;
    public int effectCount = 10;
    private Rigidbody enemyRb;
    bool isDead = false;
    public GameObject deathParticle;
    public GameObject bulletPrefab;
    void Awake()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = FindAnyObjectByType<PlayerController>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MoveTowardsPlayer();
    }

    void MoveTowardsPlayer()
    {
        if(isDead) return;
        Vector3 moveDirection = player.transform.position - transform.position;
        enemyRb.MovePosition(transform.position + moveDirection.normalized * speed * Time.deltaTime);
        enemyRb.linearVelocity = moveDirection.normalized * speed;
    }

    void TakeDamage(int damage)
    {
        health -= damage;
        CheckHealth();
    }

    void CheckHealth()
    {
        if(health <= 0 && !isDead)
        {
            isDead = true;
            StartCoroutine(Die());
        }
    }

    IEnumerator Die()
    {
        PlayDeathEffect();
        Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
        enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
        // enemyRb.linearVelocity = knockbackDirection * knockbackForce;
        yield return new WaitForSeconds(0.5f);
        PlayDeathParticle();
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

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

    void PlayDeathParticle()
    {
        GameObject deathParticleEffect = Instantiate(deathParticle, transform.position, Quaternion.identity);
        Destroy(deathParticleEffect, 1f);
    }

    void ShootBullet()
    {
        Vector3 direction = (player.transform.position - transform.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().AddForce(direction * 2.5f, ForceMode.Impulse);
    }

    void Explosion()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 5f);
        foreach (Collider nearbyObject in colliders)
        {
            if (nearbyObject.CompareTag("Player"))
            {
                // 플레이어에게 폭발 피해를 주는 로직을 여기에 작성
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("DropkickRange"))
        {
            TakeDamage(5);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Enemy") && collision.gameObject.GetComponent<EnemyTemp>().isDead)
        {
            TakeDamage(5);
        }
    }
}
