using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 폭탄 수집가 증강 효과. 적용 중에 폭탄형 적의 흡수를 땅 붕괴 대신 주변 적 처치로 바꾼다.
/// </summary>
[CreateAssetMenu(menuName = "Augment/Effects/BombCollectorEffect")]
public class BombCollectorEffect : AugmentEffect
{
    [SerializeField] private float _killRadius = 3f;

    // 처치로 생긴 죽음이 다시 이 효과에 재진입하지 않도록 막는 가드. 루프 동안 true다.
    private static bool _inChain;

    private static readonly List<Enemy> _buffer = new List<Enemy>();

    /// <summary>
    /// 현재 선택된 폭탄 수집가 효과 에셋. 없으면 null이다. EnemyExplode가 인스턴스 조회에 참조한다.
    /// </summary>
    public static BombCollectorEffect Current { get; private set; }

    /// <summary>
    /// 증강이 현재 활성(선택됨, 판 초기화 전)인지 나타낸다. EnemyExplode가 흡수 동작 분기에 참조한다.
    /// </summary>
    public static bool IsActive => Current != null;

    /// <summary>
    /// 폭탄 수집가 증강을 적용해 이 에셋을 현재 활성 효과로 등록한다.
    /// </summary>
    public override void OnApply()
    {
        Current = this;
    }

    /// <summary>
    /// 새 판 시작 시 활성 효과 등록을 해제하고 연쇄 상태를 정리한다.
    /// </summary>
    public override void OnRunReset()
    {
        Current = null;
        _inChain = false;
    }

    /// <summary>
    /// active 인스턴스의 반경 안에서 center 주변의 살아있는 다른 적들에게 Kill()을 호출한다.
    /// EnemyExplode.OnAbsorbed에서 땅 붕괴 대체 동작으로 호출한다.
    /// </summary>
    public void KillAround(Vector3 center)
    {
        if (_inChain)
        {
            return;
        }

        _inChain = true;

        // Kill() 중 예외가 발생해도 가드가 남아 이후 연쇄 처치가 영구 차단되지 않게 복구한다.
        try
        {
            _buffer.Clear();
            Collider[] hits = Physics.OverlapSphere(center, _killRadius);

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
                other.Kill();
            }
        }
        finally
        {
            _buffer.Clear();
            _inChain = false;
        }
    }
}
