using UnityEngine;
using System.Collections;

public class NoAbsortEnemy : Enemy
{
    public override IEnumerator Die(bool isKnockback)
    {
        Debug.Log("Big Guy Dead");
        PlayDeathEffect();
        // 통나무처럼 굴러감
        Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
        Vector3 pushPoint = transform.position + Vector3.up * 0.5f;
        enemyRb.AddForceAtPosition(knockbackDirection * knockbackForce, pushPoint, ForceMode.Force);
        yield return new WaitForSeconds(0.5f);
        PlayDeathParticle();    // 사망 시 나오는 모래먼지 같은 파티클 시스템 작동 함수. 
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);    // 오브젝트 풀링 사용 시 변경 필요.
    }
}
