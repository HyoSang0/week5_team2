using System.Collections;
using UnityEngine;

public class EnemyExplode : Enemy
{
    public GroundInitializer ground;
    public GameObject groundWarningEffect;
    public float radius = 3;

    void Start()
    {
        ground = GameObject.Find("GroundInitializer").GetComponent<GroundInitializer>();
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public override IEnumerator Die(bool isKnockback)
    {
        PlayDeathEffect();
        GameObject a = Instantiate(groundWarningEffect, transform.position, Quaternion.identity);
        a.GetComponent<GroundWarning>().Play(ground, transform.position.x, transform.position.z, radius);
        // 통나무처럼 굴러감
        // Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
        // Vector3 pushPoint = transform.position + Vector3.up * 0.5f;
        // enemyRb.AddForceAtPosition(knockbackDirection * knockbackForce, pushPoint, ForceMode.Force);
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);    // 오브젝트 풀링 사용 시 변경 필요.
    }
}
