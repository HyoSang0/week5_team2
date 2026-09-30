using System.Collections;
using UnityEngine;

public class NoAbsortEnemy : Enemy
{
    //연쇄 충돌에 맞는 걸로는 안 죽게 해야할 듯
    public override IEnumerator Die(bool isKnockback)
    {
        ChangeMaterial(false);
        coll.isTrigger = true;
        PlayDeathEffect();
        enemyRb.constraints = RigidbodyConstraints.FreezePositionY;
        // 통나무처럼 굴러감
        Vector3 knockbackDirection = (transform.position - player.transform.position).normalized;
        // Vector3 pushPoint = transform.position + Vector3.up * 0.5f;
        // enemyRb.AddForceAtPosition(knockbackDirection * knockbackForce, pushPoint, ForceMode.Force);
        enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(0.5f);
        coll.isTrigger = false;
        enemyRb.constraints = RigidbodyConstraints.FreezeRotationX;
        enemyRb.constraints = RigidbodyConstraints.FreezeRotationZ;
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);    // 오브젝트 풀링 사용 시 변경 필요.
    }
}
