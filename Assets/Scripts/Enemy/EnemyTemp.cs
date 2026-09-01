using UnityEngine;
using System.Collections;

public class EnemyTemp : MonoBehaviour
{
    public int health;
    public int speed = 5;
    public int knockbackForce = 5;
    private PlayerController player;
    
    public GameObject deathEffectPrefab;
    public int effectCount = 10;
    private Rigidbody enemyRb;
    bool isDead = false;
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
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    public void PlayDeathEffect()
    {
        for (int i = 0; i < effectCount; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            Instantiate(deathEffectPrefab, transform.position + randomOffset, Quaternion.identity);
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
