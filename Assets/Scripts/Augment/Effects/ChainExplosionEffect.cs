using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 연쇄 폭발 증강 효과. 적을 처치하면 반경 안의 다른 적들을 함께 처치한다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/ChainExplosionEffect")]
public class ChainExplosionEffect : AugmentEffect, IDamageSource
{
    [SerializeField] private float _radius = 2f;

    // 폭발로 생긴 처치가 다시 폭발에 재진입하지 않도록 막는 가드. 루프 동안 true다.
    private static bool _inChain;

    private static readonly List<Enemy> _buffer = new List<Enemy>();

    /// <summary>
    /// 적이 처치되면 enemy 위치 반경 radius 안의 다른 살아있는 적들에게 즉사 피해를 적용한다.
    /// 가드가 켜져 있는 동안의 처치는 다시 이 효과를 호출하지 않는다.
    /// </summary>
    public override void OnEnemyKilled(Enemy enemy)
    {
        if (_inChain)
        {
            return;
        }

        _inChain = true;

        // 피해 처리 중 예외가 발생해도 가드가 남아 이후 연쇄가 영구 차단되지 않게 복구한다.
        try
        {
            KillAround(enemy.transform.position);
        }
        finally
        {
            _inChain = false;
        }
    }

    /// <summary>
    /// 새 판 시작 시 정적 연쇄 가드를 해제해 이전 판의 예외로 남은 상태를 복구한다.
    /// </summary>
    public override void OnRunReset()
    {
        _inChain = false;
    }

    /// <summary>
    /// center 반경 radius 안의 살아있는 다른 적들을 수집해 즉사 피해를 적용한다.
    /// 수집 목록은 정적 버퍼를 재사용한다.
    /// </summary>
    private void KillAround(Vector3 center)
    {
        _buffer.Clear();
        Collider[] hits = Physics.OverlapSphere(center, _radius);

        foreach (Collider hit in hits)
        {
            Enemy other = hit.GetComponentInParent<Enemy>();
            if (other != null && !other.isDead && other.gameObject.activeInHierarchy && !_buffer.Contains(other))
            {
                _buffer.Add(other);
            }
        }

        foreach (Enemy other in _buffer)
        {
            // Kill 직접 호출 대신 즉사 피해를 보내 각 적의 기존 피해/사망 처리를 따르게 한다.
            IDamageable damageTarget = other;
            damageTarget.TakeDamage(new DamageInfo(0, DamageKind.ChainExplosion, this, isLethal: true));
        }

        _buffer.Clear();
    }
}
