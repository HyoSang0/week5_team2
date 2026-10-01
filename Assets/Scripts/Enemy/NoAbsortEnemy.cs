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
        // X·Z 회전을 한 번에 고정 (개별로 대입하면 뒤의 값으로 덮어써짐)
        enemyRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        yield return new WaitForSeconds(0.1f);
        enemyPool.DieEnemy(gameObject, poolType);    // 파괴 대신 풀로 반환
    }
}
