using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy_NoRush : Enemy
{
    [Header("Enemy Stats")]
    public int healthNr = 20;

    bool isDeadNr = false;
    bool isUnBeatNr = false;

    [Header("References")]
    public PlayerHp playerHp;
    private Rigidbody enemyRbNr;

    Material[] mat;

    private new void Awake()
    {
        enemyRbNr = GetComponent<Rigidbody>();
        player = FindAnyObjectByType<PlayerController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed;
        playerHp = player.gameObject.GetComponent<PlayerHp>();
        mat = new Material[5];

        for (int i = 0; i < 5; i++)
        { 
            mat[i]= transform.GetChild(i).GetComponent<MeshRenderer>().material;
        }

    }
    //OnTriggerEnter에서 드롭킥을 맞았는지 검사. 맞았으면 플레이어에게 반사 대미지.
    protected new void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DropkickRange"))
        {
            if(!isUnBeatNr)
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
        for(int i = 0; i < mat.Length; i++)
        {
            mat[i].color = Color.red;
        }
        yield return new WaitForSeconds(.1f);
        for(int i = 0; i < mat.Length; i++)
        {
            mat[i].color = ChangeColor("gray");

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
        enemyRbNr.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
        // enemyRb.linearVelocity = knockbackDirection * knockbackForce;
        yield return new WaitForSeconds(0.5f);
        
        yield return new WaitForSeconds(0.1f);
        ChangeColor("gray");
        enemyPool.DieEnemy(gameObject, EnemyPool.PoolType.NoRush);
    }
    protected void CheckHealthNr()
    {
        if (healthNr <= 0 && !isDeadNr)
        {
            isDeadNr = true;
            StartCoroutine(DieNr());
        }
    }

    //OnCollisionEnter에서 적이 죽은 적과 충돌했는지 검사하여 연쇄 충돌 효과 만듦.
    protected new void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && collision.gameObject.GetComponent<Enemy>().isDead)
        {
            TakeDamageNr(1);
        }
    }
}
