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
        Explode();
        // 통나무처럼 굴러감
        // Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
        // Vector3 pushPoint = transform.position + Vector3.up * 0.5f;
        // enemyRb.AddForceAtPosition(knockbackDirection * knockbackForce, pushPoint, ForceMode.Force);
        yield return new WaitForSeconds(0.1f);
        enemyPool.DieEnemy(gameObject, poolType);    // 파괴 대신 풀로 반환
    }

    /// <summary>
    /// 사망/흡수 공통 폭발 처리. 현재 위치를 중심으로 파편 효과와 GroundWarning을 생성한다.
    /// ground가 비어 있으면 GroundInitializer를 다시 찾아 사용한다.
    /// </summary>
    private void Explode()
    {
        if (ground == null)
            ground = GameObject.Find("GroundInitializer").GetComponent<GroundInitializer>();

        PlayDeathEffect();  //사망 시 나오는 파편 효과를 생성하는 함수.
        GameObject a = EffectPool.Get(groundWarningEffect, transform.position, Quaternion.identity);
        a.GetComponent<GroundWarning>().Play(ground, transform.position.x, transform.position.z, radius);
    }

    /// <summary>
    /// 흡수 사망 시 TryAbsorb의 풀 반환 전에 폭발을 즉시 실행한다.
    /// </summary>
    protected override void OnAbsorbed()
    {
        Explode();
    }
}
