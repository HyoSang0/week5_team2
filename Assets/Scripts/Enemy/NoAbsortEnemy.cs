using System.Collections;
using UnityEngine;

/// <summary>
/// 덩치
/// </summary>
public class NoAbsortEnemy : Enemy
{
    /// <summary>
    /// 통나무처럼 굴러가는 넉백 사망 처리. isKnockback과 플레이어 반대 방향을 사용해
    /// GetAugmentedKnockbackForce()의 증강 적용 넉백 힘을 충격으로 가한 뒤 풀로 반환한다.
    /// </summary>
    /// <param name="isKnockback">넉백 사망 여부</param>
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
        // 필드 knockbackForce를 직접 쓰지 않고 사용 시점에 증강을 적용해 KnockbackForce 증강이 사망 넉백에 반영되도록 한다.
        enemyRb.AddForce(knockbackDirection * GetAugmentedKnockbackForce(), ForceMode.Impulse);

        yield return new WaitForSeconds(0.5f);
        coll.isTrigger = false;
        // X·Z 회전을 한 번에 고정 (개별로 대입하면 뒤의 값으로 덮어써짐)
        enemyRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        yield return new WaitForSeconds(0.1f);
        if (isKnockback)
        {
            SpawnKillExperienceBall();
        }
        enemyPool.DieEnemy(gameObject, poolType);    // 파괴 대신 풀로 반환
    }
}
